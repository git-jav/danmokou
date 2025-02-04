﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using BagoumLib;
using BagoumLib.Expressions;
using BagoumLib.Reflection;
using Danmokou.Behavior;
using Danmokou.Core;
using Danmokou.DMath;
using Danmokou.GameInstance;
using Danmokou.Player;
using Danmokou.Reflection;
using Danmokou.Scriptables;
using Danmokou.Services;
using Danmokou.SM;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Ex = System.Linq.Expressions.Expression;
// ReSharper disable HeuristicUnreachableCode
#pragma warning disable 162

namespace Danmokou.Expressions {
public static class BakeCodeGenerator {
    public class DMKObjectPrinter : CSharpObjectPrinter {

        public override string Print(object? o) => FormattableString.Invariant(o switch {
            V2RV2 rv2 => 
                $"new V2RV2({rv2.nx}f, {rv2.ny}f, {rv2.rx}f, {rv2.ry}f, {rv2.angle}f)",
            Vector2 v2 =>
                $"new Vector2({v2.x}f, {v2.y}f)",
            Vector3 v3 =>
                $"new Vector3({v3.x}f, {v3.y}f, {v3.z}f)",
            Vector4 v4 =>
                $"new Vector4({v4.x}f, {v4.y}f, {v4.z}f, {v4.w}f)",
            CCircle c =>
                $"new CCircle({c.x}f, {c.y}f, {c.r}f)",
            CRect r =>
                $"new CRect({r.x}f, {r.y}f, {r.halfW}f, {r.halfH}f, {r.angle}f)",
            _ => $"{base.Print(o)}"
        });
    }

    /// <summary>
    /// A context responsible for either saving or loading all code generation in the game state.
    /// </summary>
    public class CookingContext {
        private const string outputPath = "Assets/Danmokou/Plugins/Danmokou/Danmaku/Expressions/Generated/";
        private const string nmSpace = "Danmokou.Expressions";
        private const string clsName = "GeneratedExpressions_CG";
        private const string header = @"//----------------------
// <auto-generated>
//     Generated by Danmokou expression baking for use on AOT/IL2CPP platforms.
// </auto-generated>
//----------------------
//#if EXBAKE_LOAD
using System;
using System.Collections.Generic;
using UnityEngine;
using BagoumLib.Cancellation;
using BagoumLib.Mathematics;
using Danmokou.Behavior;
using Danmokou.Core;
using Danmokou.Danmaku;
using Danmokou.DMath;
using Danmokou.DataHoist;
using Danmokou.Graphics;
using Danmokou.Player;
using Danmokou.Services;
using Danmokou.SM;
#pragma warning disable 162
#pragma warning disable 219";
        private const string footer = "//#endif";
        // ReSharper disable once CollectionNeverQueried.Local
        private List<ExportedFile> GeneratedFiles { get; } = new();
        private HashSet<string> OpenedFileKeys { get; } = new();
        public Stack<FileContext> OpenContexts { get; } = new();
        public FileContext? CurrentFile => OpenContexts.TryPeek();
        public FileContext.Baker? CurrentBake => CurrentFile == null ? null :
            (CurrentFile is FileContext.Baker fbc) ? fbc :
            throw new Exception("Current context is not a bake");
        public FileContext.Server? CurrentServe => CurrentFile == null ? null :
            (CurrentFile is FileContext.Server fsc) ? fsc :
            throw new Exception("Current context is not a serve");

        public IDisposable NewContext(KeyType type, string key) {
        #if EXBAKE_SAVE
            var fileCtx = new FileContext.Baker(this, type, key);
            //It's still beneficial to open a duplicate context for the sake of record-keeping
            if (OpenedFileKeys.Contains(fileCtx.FileIdentifier))
                fileCtx.DoNotExport = true;
        #else
            var fileCtx = new FileContext.Server(this, type, key);
        #endif
            OpenedFileKeys.Add(fileCtx.FileIdentifier);
            OpenContexts.Push(fileCtx);
            return fileCtx;
        }

        private void DisposeBake(FileContext.Baker fbc) {
            if (fbc != CurrentFile) throw new Exception("Tried to dispose the wrong FileBakeContext");
            if (fbc == null) throw new Exception("Dispose FileBakeContext should not be null");
            if (fbc.Export().Try(out var gf))
                GeneratedFiles.Add(gf);
            OpenContexts.Pop();
        }
        private void DisposeServe(FileContext.Server fbc) {
            if (fbc != CurrentFile) throw new Exception("Tried to dispose the wrong FileServeContext");
            if (fbc == null) throw new Exception("Dispose FileServeContext should not be null");
            //No extra action needs to be taken
            OpenContexts.Pop();
        }

        private const int FUNCS_PER_FILE = 300;

        public void Export() {
#if EXBAKE_SAVE
            var generatedCount = 0;
            var currentFuncs = new List<string>();
            void ExportFuncs() {
                FileUtils.WriteString(Path.Combine(outputPath, $"Generated{generatedCount++}.cs"), 
                    WrapInClass(string.Join("\n", currentFuncs)));
                currentFuncs.Clear();
            }
            void AddFuncs(IEnumerable<string> funcs) {
                foreach (var f in funcs) {
                    currentFuncs.Add(f);
                    if (currentFuncs.Count >= FUNCS_PER_FILE) {
                        ExportFuncs();
                    }
                }
            }
            var dictSB = new StringBuilder();
            dictSB.AppendLine($"\tstatic {clsName}() {{");
            dictSB.AppendLine("\t_allDataMap = " +
                              "new Dictionary<string, List<object>>() {");
            
            foreach (var gf in GeneratedFiles) {
                AddFuncs(gf.funcDefs);
                var funcs = $"new List<object>() {{\n\t{string.Join(",\n\t", gf.funcsAsObjects)}\n}}";
                dictSB.AppendLine($"\t{{ \"{gf.filename}\", {funcs.Replace("\n", "\n\t")} }},");
            }
            ExportFuncs();
            dictSB.AppendLine("\t};");
            dictSB.AppendLine("}");
            FileUtils.WriteString(Path.Combine(outputPath, "Top.cs"), WrapInClass(dictSB.ToString()));
            GeneratedFiles.Clear();
#endif
        }

        private static string WrapInClass(string inner) =>
            $@"{header}

namespace {nmSpace} {{
internal static partial class {clsName} {{
{inner.Replace("\n", "\n\t")}
}}
}}
{footer}
";

        public readonly struct ExportedFile {
            public readonly KeyType type;
            public readonly string filename;
            public readonly IEnumerable<string> funcDefs;
            public string FuncText => string.Join("\n", funcDefs);
            public readonly IEnumerable<string> funcsAsObjects; 
            
            public ExportedFile(KeyType type, string filename, IEnumerable<string> funcDefs, IEnumerable<string> funcsAsObjects) {
                this.type = type;
                this.filename = filename;
                this.funcDefs = funcDefs;
                this.funcsAsObjects = funcsAsObjects;
            }
        }

        public enum KeyType {
            /// <summary>
            /// SM.CreateFromDump
            /// </summary>
            SM,
            /// <summary>
            /// string.Into
            /// </summary>
            INTO,
            /// <summary>
            /// Reflection with ReflCtx and a func argument requiring compilation
            /// </summary>
            MANUAL
        }

        public abstract class FileContext : IDisposable {
            protected readonly CookingContext parent;
            protected readonly KeyType keyType;
            private readonly object key;
            public FileContext(CookingContext parent, KeyType keyType, object key) {
                this.parent = parent;
                this.keyType = keyType;
                this.key = key;
            }

            public string FileIdentifier => string.Format("{0}{1}", keyType switch {
                KeyType.SM => "Sm",
                KeyType.INTO => "Into",
                _ => "Manual"
            }, (long)key.GetHashCode() + (long)int.MaxValue);

            public abstract void Dispose();
            
            
            /// <summary>
            /// A context that records generated functions in a file and eventually prints them to source code.
            /// </summary>
            public class Baker : FileContext {
                public bool DoNotExport { get; set; } = false;
                public ITypePrinter TypePrinter { get; set; } = new CSharpTypePrinter();
                private List<(string text, string fnName, Type returnType, (Type typ, string argName)[] argDefs)> GeneratedFunctions { get; } = new();

                public Baker(CookingContext parent, KeyType keyType, object key) : base(parent, keyType, key) { }
                public ExportedFile? Export() => (DoNotExport || GeneratedFunctions.Count == 0) ?
                    (ExportedFile?)null :
                    new ExportedFile(keyType, FileIdentifier, ExportFuncDefs(), 
                        GeneratedFunctions.Select(f => 
                            "(Func<" +
                            string.Concat(f.argDefs
                                .Select(ts => TypePrinter.Print(ts.typ) + ", ")) + TypePrinter.Print(f.returnType)
                            + $">){f.fnName}"));

                private IEnumerable<string> ExportFuncDefs() => GeneratedFunctions.Select(f => $@"
private static {TypePrinter.Print(f.returnType)} {f.fnName}({string.Join(", ",
                    f.argDefs.Select(arg => $"{TypePrinter.Print(arg.typ)} {arg.argName}"))}) {{
    {f.text.Trim().Replace("\n", "\n\t")}
}}");

                private string MakeFuncName(string prefix, int index) => $"{prefix}_{index}";

                public void Add<D>(string fnText, (Type, string)[] argDefs) {
                    var name = MakeFuncName(FileIdentifier, GeneratedFunctions.Count);
                    GeneratedFunctions.Add((fnText, name, typeof(D), argDefs));
                }

                public override void Dispose() {
                    parent.DisposeBake(this);
                }
            }
            

            /// <summary>
            /// A proxy that retrieves functions from a source code file that was generated by Baker.
            /// </summary>
            public class Server : FileContext {
                private readonly List<object> compiled;
                private int index = 0;
            
                public Server(CookingContext parent, KeyType keyType, object key) : base(parent, keyType, key) {
                    this.compiled = GeneratedExpressions.RetrieveBakedOrEmpty(FileIdentifier);
                }

                public D Next<D>(object[] proxyArgs) {
                    if (index >= compiled.Count) {
                        if (compiled.Count == 0)
                            throw new Exception($"File {FileIdentifier} has no baked expressions, but one was requested");
                        throw new Exception($"Not enough baked expressions for file {FileIdentifier}");
                    }
                    var func = compiled[index++];
                    var invoker = func.GetType().GetMethod("Invoke")!;
                    var obj = invoker.Invoke(func, proxyArgs);
                    if (obj is D del) 
                        return del;
                    throw new Exception($"Baked expression #{index}/{compiled.Count} for file {FileIdentifier} " +
                                        $"is of type {obj.GetType().RName()}, requested {typeof(D).RName()}");
                }
            
                public override void Dispose() {
                    parent.DisposeServe(this);
                }
            }
            
        }
    }


    public static CookingContext Cook { get; } = new();


    public static IDisposable? OpenContext(CookingContext.KeyType type, string identifier) =>
#if EXBAKE_SAVE || EXBAKE_LOAD
        Cook.NewContext(type, identifier);
#else
        null;
#endif


    private static readonly Dictionary<object, Expression> DefaultObjectReplacements =
        new() {
            {ExMHelpers.LookupTable, ExMHelpers.exLookupTable}
        };

#if EXBAKE_SAVE
    /// <summary>
    /// Returns a function that hoists objects such as timers and updates the object-mapping dict accordingly.
    /// </summary>
    private static Func<Dictionary<object, Expression>, object, Ex> GeneralConstHandling(CookingContext baker, TExArgCtx tac) {
        return (dct, obj) => {
            if (obj is ETime.Timer t) {
                var key_name = tac.Ctx.NameWithSuffix("timer");
                tac.Ctx.HoistedVariables.Add(FormattableString.Invariant(
                    $"var {key_name} = ETime.Timer.GetTimer(\"{t.name}\");"));
                return dct[obj] = Ex.Variable(typeof(ETime.Timer), key_name);
            } else if (obj is BEHPointer p) {
                var key_name = tac.Ctx.NameWithSuffix("behp");
                tac.Ctx.HoistedVariables.Add(FormattableString.Invariant(
                    $"var {key_name} = BehaviorEntity.GetPointerForID(\"{p.id}\");"));
                return dct[obj] = Ex.Variable(typeof(BEHPointer), key_name);
            } else
                return Ex.Constant(obj);
        };
    }
#endif
    public static D BakeAndCompile<D>(this TEx ex, TExArgCtx tac, params ParameterExpression[] prms) {
#if EXBAKE_LOAD
        return (Cook.CurrentServe ?? throw new Exception("Tried to load an expression with no active serve"))
            .Next<D>(tac.Ctx.ProxyArguments.ToArray());
#endif
        //TODO:Linux
        var f = FlattenVisitor.Flatten(ex, true, true);
        //Logs.Log($"Ex:{typeof(D).RName()} " +
        //         $"{new ExpressionPrinter{ObjectPrinter = new DMKObjectPrinter()}.LinearizePrint(f)}");
        var result = Ex.Lambda<D>(f, prms).Compile();
#if EXBAKE_SAVE
        var printer = new ExpressionPrinter() {ObjectPrinter = new DMKObjectPrinter()};
        var sb = new StringBuilder();
        //Replace must be first to handle private hoisting, otherwise flatten might reconstruct the expressions
        var constReplaced = new ReplaceExVisitor(
            tac.Ctx.HoistedReplacements,
            DefaultObjectReplacements,
            GeneralConstHandling(Cook, tac)).Visit(ex);
        var flattened = FlattenVisitor.Flatten(constReplaced, true, false);
        //Run replacement again to handle the method simplification for cos/sin
        var constReplaced2 = new ReplaceExVisitor(
            tac.Ctx.HoistedReplacements,
            DefaultObjectReplacements,
            GeneralConstHandling(Cook, tac)).Visit(flattened);
        var linearized = new LinearizeVisitor().Visit(constReplaced2);
        //As the replaced EXs contain references to nonexistent variables, we don't want to actually compile it
        var rex = Ex.Lambda<D>(linearized, prms);
        foreach (var hoistVar in tac.Ctx.HoistedVariables) {
            sb.AppendLine(hoistVar);
        }
        sb.Append("return ");
        sb.Append(printer.Print(rex));
        sb.AppendLine(";");
        (Cook.CurrentBake ?? throw new Exception("An expression was compiled with no active bake"))
            .Add<D>(sb.ToString(), tac.Ctx.ProxyTypes.Select(t => (t, tac.Ctx.NextProxyArg())).ToArray());
#endif
        return result;
    }
    
#if UNITY_EDITOR
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public static void BakeExpressions() {
        //These calls ensure that static reflections are correctly initialized
        new Challenge.WithinC(0);
        new Challenge.WithoutC(0);
        
        
        var typFieldsCache = new Dictionary<Type, List<(MemberInfo, ReflectIntoAttribute)>>();
        void LoadReflected(UnityEngine.Object go) {
            var typ = go.GetType();
            if (!typFieldsCache.TryGetValue(typ, out var members)) {
                members = typFieldsCache[typ] = new List<(MemberInfo, ReflectIntoAttribute)>();
                foreach (var m in typ.GetFields().Cast<MemberInfo>().Concat(typ.GetProperties())) {
                    foreach (var c in m.GetCustomAttributes()) {
                        if (c is ReflectIntoAttribute ra) {
                            members.Add((m, ra));
                        }
                    }
                }
            }
            foreach (var (m, ra) in members) {
                var val = (m is FieldInfo f) ? f.GetValue(go) : (m as PropertyInfo)!.GetValue(go);
                if (ra.resultType != null) {
                    if (val is string[] strs)
                        strs.ForEach(s => s.IntoIfNotNull(ra.resultType));
                    else if (val is string str)
                        str.IntoIfNotNull(ra.resultType);
                    else if (val is RString rs)
                        rs.Get().IntoIfNotNull(ra.resultType);
                    else if (val is null) {
                        //generally caused by unfilled field, can be ignored.
                    } else
                        throw new Exception("ReflectInto has resultType set on an invalid property type: " +
                                            $"{typ.RName()}.{m.Name}<{val.GetType().RName()}/" +
                                            $"{ra.resultType.RName()}>");
                }
            }
        }
        Logs.Log("Loading GameObject reflection properties...");
        AssetDatabase.FindAssets("t:GameObject")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .SelectMany(go => go.GetComponentsInChildren(typeof(Component)))
            .ForEach(LoadReflected);
        Logs.Log("Loading ScriptableObject reflection properties...");
        AssetDatabase.FindAssets("t:ScriptableObject")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
            .ForEach(LoadReflected);
        Logs.Log("Loading TextAssets for reflection...");
        var textAssets = AssetDatabase.FindAssets("t:TextAsset", 
            GameManagement.References.scriptFolders.Prepend("Assets/Danmokou/Patterns").ToArray())
            .Select(AssetDatabase.GUIDToAssetPath);
        foreach (var path in textAssets) {
            try {
                if (path.EndsWith(".txt")) {
                    Logs.Log($"Loading script from file {path}");
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    StateMachineManager.FromText(textAsset);
                }
            } catch (Exception e) {
                Logs.UnityError($"Failed to parse {path}:\n" + Exceptions.FlattenNestedException(e).Message);
            }
        }
        Logs.Log("Invoking ReflWrap wrappers...");
        ReflWrap.InvokeAllWrappers();
        Logs.Log("Exporting reflected code...");
        BakeCodeGenerator.Cook.Export();
        Logs.Log("Expression baking complete.");
    }
#endif
}
}
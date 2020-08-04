﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Danmaku;
using DMath;
using JetBrains.Annotations;
using UnityEngine;

namespace Danmaku {

[SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
public class GenCtx : IDisposable {
    public readonly Dictionary<string, float> fs = new Dictionary<string, float>();
    public readonly Dictionary<string, Vector2> v2s = new Dictionary<string, Vector2>();

    public float GetFloatOrThrow(string key) {
        if (key == "i") return i;
        if (key == "pi") return pi;
        return fs.GetOrThrow(key, "GCX float values");
    }

    public IReadOnlyDictionary<string, Vector2> V2s => v2s;
    public readonly Dictionary<string, Vector3> v3s = new Dictionary<string, Vector3>();
    public IReadOnlyDictionary<string, Vector3> V3s => v3s;
    public readonly Dictionary<string, V2RV2> rv2s = new Dictionary<string, V2RV2>();
    public IReadOnlyDictionary<string, V2RV2> RV2s => rv2s;
    public readonly List<(Reflector.ExType, string)> exposed = new List<(Reflector.ExType, string)>();
    /// <summary>
    /// Loop iteration
    /// </summary>
    public int i = 0;
    /// <summary>
    /// Parent loop iteration
    /// </summary>
    public int pi = 0;
    /// <summary>
    /// Firing index
    /// </summary>
    public int index = 0;
    /// <summary>
    /// Firing BEH (copied from DelegatedCreator)
    /// </summary>
    public BehaviorEntity exec;
    public V2RV2 RV2 {
        get => rv2s["rv2"];
        set => rv2s["rv2"] = value;
    }
    public V2RV2 BaseRV2 {
        get => rv2s["brv2"];
        set => rv2s["brv2"] = value;
    }
    public float SummonTime {
        get => fs["st"];
        set => fs["st"] = value;
    }
    public Vector2 Loc => exec.GlobalPosition();
    public ParametricInfo AsBPI => ParametricInfo.WithRandomId(Loc, index, (float) i);
    private static readonly Stack<GenCtx> cache = new Stack<GenCtx>();
    private GenCtx() { }

    public uint NextID() => RNG.GetUInt();

    public static GenCtx New(BehaviorEntity exec, V2RV2 rv2) {
        //if (cache.Count == 0) Log.Unity("Instantiating new GCX", true, Log.Level.DEBUG1);
        var newgc = (cache.Count > 0) ? cache.Pop() : new GenCtx();
        newgc.exec = exec;
        newgc.RV2 = newgc.BaseRV2 = rv2;
        newgc.SummonTime = 0;
        return newgc;
    }

    public void OverrideScope(BehaviorEntity nexec, V2RV2 rv2, int ind) {
        exec = nexec;
        RV2 = BaseRV2 = rv2;
        index = ind;
    }

    public void Dispose() {
        fs.Clear();
        v2s.Clear();
        v3s.Clear();
        rv2s.Clear();
        exposed.Clear();
        i = 0;
        pi = 0;
        exec = null;
        cache.Push(this);
    }

    public GenCtx Copy() {
        var cp = New(exec, RV2);
        this.fs.CopyInto(cp.fs);
        this.v2s.CopyInto(cp.v2s);
        this.v3s.CopyInto(cp.v3s);
        this.rv2s.CopyInto(cp.rv2s);
        cp.exposed.AddRange(this.exposed);
        cp.BaseRV2 = RV2; //this gets overwritten by copyinto...
        cp.i = cp.pi = this.i;
        cp.index = this.index;
        return cp;
    }

    public GenCtx Copy(V2RV2 rv2) {
        var cp = Copy();
        cp.RV2 = cp.BaseRV2 = rv2;
        return cp;
    }

    public void FinishIteration(List<GCRule> postloop, float angleIncrement) {
        UpdateRules(postloop);
        RV2 += angleIncrement;
        ++i;
    }

    private bool TryGetType(ReferenceMember refr, out Reflector.ExType ext) {
        ext = default;
        if (fs.ContainsKey(refr.var)) ext = Reflector.ExType.Float;
        else if (v2s.ContainsKey(refr.var)) ext = Reflector.ExType.V2;
        else if (v3s.ContainsKey(refr.var)) ext = Reflector.ExType.V3;
        else if (rv2s.ContainsKey(refr.var)) ext = Reflector.ExType.RV2;
        else return false;
        return true;
    }

    private void UpdateRule(GCRule rule) {
        if (!TryGetType(rule.refr, out var variableType)) variableType = rule.exType;
        if (rule is GCRule<float> rf) {
            float value = rf.Evaluate(this);
            if (rule.refr.var == "_") return;
            if (variableType == Reflector.ExType.Float) fs[rule.refr.var] = rule.refr.Resolve(fs, value, rule.op);
            else if (variableType == Reflector.ExType.V2)
                v2s[rule.refr.var] = rule.refr.ResolveMembers(v2s, value, rule.op);
            else if (variableType == Reflector.ExType.V3)
                v3s[rule.refr.var] = rule.refr.ResolveMembers(v3s, value, rule.op);
            else if (variableType == Reflector.ExType.RV2)
                rv2s[rule.refr.var] = rule.refr.ResolveMembers(rv2s, value, rule.op);
            else throw new Exception($"Can't assign float to {variableType}");
        } else if (rule is GCRule<Vector2> r2) {
            Vector2 value = r2.Evaluate(this);
            if (rule.refr.var == "_") return;
            if (variableType == Reflector.ExType.V2) v2s[rule.refr.var] = rule.refr.ResolveMembers(v2s, value, rule.op);
            else if (variableType == Reflector.ExType.V3)
                v3s[rule.refr.var] = rule.refr.ResolveMembers(v3s, value, rule.op);
            else throw new Exception($"Can't assign V2 to {variableType}");
        } else if (rule is GCRule<Vector3> r3) {
            Vector3 value = r3.Evaluate(this);
            if (rule.refr.var == "_") return;
            if (variableType == Reflector.ExType.V3) v3s[rule.refr.var] = rule.refr.ResolveMembers(v3s, value, rule.op);
            else throw new Exception($"Can't assign V2 to {variableType}");
        } else if (rule is GCRule<V2RV2> rrv) {
            V2RV2 value = rrv.Evaluate(this);
            if (rule.refr.var == "_") return;
            if (variableType == Reflector.ExType.RV2)
                rv2s[rule.refr.var] = rule.refr.ResolveMembers(rv2s, value, rule.op);
            else throw new Exception($"Can't assign RV2 to {variableType}");
        }
    }

    public void UpdateRules([CanBeNull] List<GCRule> rules) {
        if (rules == null) return;
        for (int ii = 0; ii < rules.Count; ++ii) UpdateRule(rules[ii]);
    }
}

public enum GCOperator {
    /// <summary>
    /// =
    /// </summary>
    Assign,
    /// <summary>
    /// +=
    /// </summary>
    AddAssign,
    /// <summary>
    /// -=
    /// </summary>
    SubAssign,
    /// <summary>
    /// *=
    /// </summary>
    MulAssign,
    /// <summary>
    /// /=
    /// </summary>
    DivAssign,
    /// <summary>
    /// //=
    /// </summary>
    FDivAssign
}

public abstract class GCRule {
    public readonly ReferenceMember refr;
    public readonly Reflector.ExType exType;
    public readonly GCOperator op;

    protected GCRule(Reflector.ExType ext, ReferenceMember rf, GCOperator op) {
        refr = rf;
        exType = ext;
        this.op = op;
    }
}
public class GCRule<T> : GCRule {
    private readonly GCXF<T> func;

    public GCRule(Reflector.ExType ext, ReferenceMember rf, GCOperator op, GCXF<T> f) : base(ext, rf, op) {
        func = f;
    }

    public T Evaluate(GenCtx gcx) => func(gcx);
}
}
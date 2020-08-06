﻿using System;
using System.Collections.Generic;
using System.Linq;
using DMath;
using JetBrains.Annotations;
using SM;
using UnityEngine;
using ExBPY = System.Func<DMath.TExPI, TEx<float>>;
using ExBPRV2 = System.Func<DMath.TExPI, TEx<DMath.V2RV2>>;
using ExTP = System.Func<DMath.TExPI, TEx<UnityEngine.Vector2>>;
using ExPred = System.Func<DMath.TExPI, TEx<bool>>;
using static Danmaku.GenCtxUtils;
using static Danmaku.Enums;
using static Danmaku.GenCtxProperty;
using static Compilers;

namespace Danmaku {

/// <summary>
/// Properties that modify the behavior of generic repeater commands (gtrepeat, girepeat, gcrepeat, gsrepeat).
/// </summary>
public class GenCtxProperty {
    /// <summary>
    /// Dummy property that does nothing.
    /// </summary>
    public static GenCtxProperty NoOp() => new CompositeProp();
    /// <summary>
    /// Set the number of times a repeater will run. Resolved after start rules.
    /// </summary>
    /// <param name="times"></param>
    /// <returns></returns>
    [Alias("t")]
    public static GenCtxProperty Times(GCXF<float> times) => new TimesProp(null, times);
    /// <summary>
    /// Set the maximum number of times a repeater will run.
    /// </summary>
    /// <returns></returns>
    [Alias("maxt")]
    public static GenCtxProperty MaxTimes(int max) => new TimesProp(max, null);

    /// <summary>
    /// Set the number of times a repeater will run, along with the max times. Resolved after start rules.
    /// </summary>
    /// <param name="max"></param>
    /// <param name="times"></param>
    /// <returns></returns>
    public static GenCtxProperty TM(int max, GCXF<float> times) => new TimesProp(max, times);
    /// <summary>
    /// A combination of TM with a fixed times count, and mod parametrization.
    /// </summary>
    /// <param name="times">Times count</param>
    /// <returns></returns>
    public static GenCtxProperty TMMod(FXY times) {
        var t = (int)times(0);
        return new CompositeProp(new TimesProp(t, _ => t),
            new ParametrizationProp(Enums.Parametrization.MOD, null));
    }

    /// <summary>
    /// A combination of TM with a fixed times count, and inverse mod parametrization.
    /// </summary>
    /// <param name="times">Times count</param>
    /// <returns></returns>
    public static GenCtxProperty TMIMod(FXY times) {
        var t = (int)times(0);
        return new CompositeProp(new TimesProp(t, _ => t),
            new ParametrizationProp(Enums.Parametrization.INVMOD, null));
    }

    /// <summary>
    /// Set the number of frames a repeater will wait between invocations. Resolved after invocation, before post-loop.
    /// Not allowed for SyncPattern.
    /// </summary>
    /// <param name="frames"></param>
    /// <returns></returns>
    [Alias("w")]
    public static GenCtxProperty Wait(GCXF<float> frames) => new WaitProp(frames);
    /// <summary>
    /// Set the max amount of time this function will execute for, in frames.
    /// </summary>
    public static GenCtxProperty For(GCXF<float> frames) => new ForTimeProp(frames);
    /// <summary>
    /// Wait and times properties combined.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty WT(GCXF<float> frames, GCXF<float> times) => new CompositeProp(Wait(frames), Times(times));
    /// <summary>
    /// Wait and times properties combined, where Wait is divided by difficulty and Times is multiplied by difficulty.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty WTd(ExBPY difficulty, ExBPY frames, ExBPY  times) => new CompositeProp(
        Wait(GCXFf(b => frames(b).Div(difficulty(b)))), 
        Times(GCXFf(b => times(b).Mul(difficulty(b)))));
    
    /// <summary>
    /// Wait and TM properties combined.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty WTM(GCXF<float> frames, int max, GCXF<float> times) => new CompositeProp(Wait(frames), TM(max, times));
    /// <summary>
    /// Wait, times, rpp properties combined.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty Async(GCXF<float> frames, GCXF<float> times, GCXF<V2RV2> incr) => new CompositeProp(Wait(frames), Times(times), RV2Incr(incr));
    /// <summary>
    /// Wait, times, rpp properties combined, where Wait is divided by difficulty and Times is multiplied by difficulty.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty AsyncD(ExBPY difficulty, ExBPY frames, ExBPY  times, GCXF<V2RV2> incr) => 
        new CompositeProp(WTd(difficulty, frames, times), RV2Incr(incr));
    
    /// <summary>
    /// Wait, times, rpp properties combined, where Wait is divided by, Times is multiplied by, and rpp is divided by difficulty.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty AsyncDR(ExBPY difficulty, ExBPY frames, ExBPY  times, ExBPRV2 incr) => 
        new CompositeProp(WTd(difficulty, frames, times), RV2Incr(GCXFrv2(b => incr(b).Div(difficulty(b)))));
    
    /// <summary>
    /// Wait, FOR, rpp properties combined. Times is set to infinity.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty AsyncFor(GCXF<float> frames, GCXF<float> runFor, GCXF<V2RV2> incr) => new CompositeProp(Wait(frames), Times(GCXFRepo.Max), For(runFor), RV2Incr(incr));
    /// <summary>
    /// Times, rpp properties combined.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty Sync(GCXF<float> times, GCXF<V2RV2> incr) => new CompositeProp(Times(times), RV2Incr(incr));
    /// <summary>
    /// Times, rpp properties combined, where Times is multiplied by and rpp is divided by difficulty.
    /// </summary>
    public static GenCtxProperty SyncDR(ExBPY difficulty, ExBPY times, ExBPRV2 incr) => new CompositeProp(
        Times(GCXFf(b => times(b).Mul(difficulty(b)))), 
        RV2Incr(GCXFrv2(b => incr(b).Div(difficulty(b)))));
    
    
    /// <summary>
    /// Set the delay before the repeater's first invocation. Resolved after start rules.
    /// Not allowed for SyncPattern.
    /// </summary>
    /// <param name="frames"></param>
    /// <returns></returns>
    public static GenCtxProperty Delay(GCXF<float> frames) => new DelayProp(frames);
    /// <summary>
    /// Flag that forces the repeater to wait for the child invocation to finish
    /// before continuing.
    /// GIRepeat/GTRepeat only.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty WaitChild() => new WaitChildProp();
    /// <summary>
    /// When executing a function with multiple children,
    /// instead of executing all of them simultaneously,
    /// execute only the one given by the indexer.
    /// </summary>
    /// <param name="indexer"></param>
    /// <returns></returns>
    public static GenCtxProperty Alternate(GCXF<float> indexer) => new AlternateProp(indexer);
    /// <summary>
    /// Causes all objects to be summoned in world space relative to an origin.
    /// Resolved before start rules.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty Root(GCXF<Vector2> root) => new RootProp(root, false);
    /// <summary>
    /// Causes all objects to be summoned in world space relative to an origin.
    /// Adjusts the nonrotational offset of the RV2 so the final summoning position is unaffected.
    /// Resolved before start rules.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty RootAdjust(GCXF<Vector2> root) => new RootProp(root, true);
    /// <summary>
    /// Before start rules, move the current V2RV2 into nonrotational coordinates only,
    /// inheriting the angle, and set a new offset. This is useful for doing inner repeats.
    /// </summary>
    /// <param name="newOffset"></param>
    /// <returns></returns>
    public static GenCtxProperty Bank(GCXF<V2RV2> newOffset) => new BankProp(false, newOffset);
    /// <summary>
    /// Before start rules, move the current V2RV2 into nonrotational coordinates only,
    /// setting the angle to zero, and set a new offset. This is useful for doing inner repeats.
    /// </summary>
    /// <param name="newOffset"></param>
    /// <returns></returns>
    public static GenCtxProperty Bank0(GCXF<V2RV2> newOffset) => new BankProp(true, newOffset);
    /// <summary>
    /// Rules that are run before any invocations.
    /// </summary>
    /// <param name="rules"></param>
    /// <returns></returns>
    public static GenCtxProperty Start(GCRule[] rules) => new StartProp(rules);
    /// <summary>
    /// Rules that are run every loop, after `i` is set for the loop, and before the invocation.
    /// </summary>
    /// <param name="rules"></param>
    /// <returns></returns>
    public static GenCtxProperty PreLoop(GCRule[] rules) => new PreLoopProp(rules);
    /// <summary>
    /// Rules that are run every loop, after the invocation and after waiting is complete.
    /// </summary>
    /// <param name="rules"></param>
    /// <returns></returns>
    public static GenCtxProperty PostLoop(GCRule[] rules) => new PostLoopProp(rules);
    /// <summary>
    /// Rules that are run when the repeater is done.
    /// </summary>
    /// <param name="rules"></param>
    /// <returns></returns>
    public static GenCtxProperty End(GCRule[] rules) => new EndProp(rules);
    /// <summary>
    /// Increment the RV2 by a certain amount every loop. Resolved after PostLoop.
    /// </summary>
    /// <param name="rule"></param>
    /// <returns></returns>
    [Alias("rpp")]
    public static GenCtxProperty RV2Incr(GCXF<V2RV2> rule) => new RV2IncrProp(rule);
    /// <summary>
    /// Increment the RV2 by 360/{times} every loop.
    /// You can use this with RV2Incr, this one will take effect second.
    /// </summary>
    public static GenCtxProperty Circle() => new RV2CircleTag();
    /// <summary>
    /// Spread the RV2 evenly over a total width, so each increment is totalWidth/({times}-1).
    /// </summary>
    public static GenCtxProperty Spread(GCXF<V2RV2> totalWidth) => new RV2SpreadProp(totalWidth);

    /// <summary>
    /// Right before invocation, applies a contortion to RV2.a, which is undone after the invocation.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty MutateAng(GCXF<float> f) => new MAngProp(f);
    /// <summary>
    /// TIMES and CIRCLE props combined.
    /// </summary>
    [Alias("tc")]
    public static GenCtxProperty TimesCircle(GCXF<float> times) => new CompositeProp(Times(times), Circle());
    /// <summary>
    /// Set the RV2 angle to a value. Resolved after PreLoop and before the invocation.
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    public static GenCtxProperty FRV2(GCXF<V2RV2> f) => new FRV2Prop(f);
    /// <summary>
    /// Flag that indicates the global rotation of all summoned bullets.
    /// </summary>
    /// <param name="facing"></param>
    /// <returns></returns>
    public static GenCtxProperty Face(Facing facing) => new ValueProp<Facing>(facing);
    /// <summary>
    /// Play an SFX on every loop iteration, looping through the given array. This is run right before the invocation.
    /// </summary>
    public static GenCtxProperty SFX(string[] sfx) => new SFXProp(sfx, null, null);
    /// <summary>
    /// Play an SFX on every loop iteration, using the indexer function to select one. This is run right before the invocation.
    /// </summary>
    public static GenCtxProperty SFXf(string[] sfx, GCXF<float> indexer) => new SFXProp(sfx, indexer, null);
    /// <summary>
    /// Play an SFX on every loop iteration if the predicate is true,
    /// using the indexer function to select one. This is run right before the invocation.
    /// </summary>
    public static GenCtxProperty SFXfIf(string[] sfx, GCXF<float> indexer, GCXF<bool> pred) => new SFXProp(sfx, indexer, pred);
    /// <summary>
    /// Play an SFX on every loop iteration if the predicate is true. This is run right before the invocation.
    /// </summary>
    public static GenCtxProperty SFXIf(string[] sfx, GCXF<bool> pred) => new SFXProp(sfx, null, pred);
    /// <summary>
    /// Set the parameterization method of the loop. Default is DEFER.
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    [Alias("p")]
    public static GenCtxProperty Parametrization(Parametrization p) => new ParametrizationProp(p, null);

    /// <summary>
    /// Set the parameterization method of the loop. Default is DEFER. Also mutate the parent index.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="mutater"></param>
    /// <returns></returns>
    [Alias("pm")]
    public static GenCtxProperty MutateParametrization(Parametrization p, GCXF<float> mutater) => new ParametrizationProp(p, mutater);
    /// <summary>
    /// Cycle between color on every invocation.
    /// </summary>
    public static GenCtxProperty Color(string[] colors) => new ColorProp(colors, null, false);
    /// <summary>
    /// Cycle between color on every invocation. Merge colors in the reverse direction.
    /// </summary>
    public static GenCtxProperty ColorR(string[] colors) => new ColorProp(colors, null, true);

    /// <summary>
    /// Select a color on every invocation by running the indexer function.
    /// </summary>
    /// <param name="colors"></param>
    /// <param name="indexer">Indexer function</param>
    /// <returns></returns>
    public static GenCtxProperty Colorf(string[] colors, GCXF<float> indexer) => new ColorProp(colors, indexer, false);

    /// <summary>
    /// Summon successive objects along an offset function.
    /// Note: As with all functions here, you can use `t` or `&amp;i` to get the
    /// iteration number of the loop in the given functions.
    /// </summary>
    /// <param name="sah"></param>
    /// <param name="angleOffset"></param>
    /// <param name="nextLocation"></param>
    /// <returns></returns>
    public static GenCtxProperty SAOffset(SAAngle sah, GCXF<float> angleOffset, GCXF<Vector2> nextLocation) => new SAHandlerProp(new SAOffsetHandler(sah, angleOffset, nextLocation));
    /// <summary>
    /// Target a specific location in world space. Targeting is performed once, before start rules.
    /// The targeting is performed from the executing entity.
    /// Functionality depends on Method:
    /// <br/>Angle = point at the location
    /// <br/>NX/RX = add to the non/rotational X component the delta X to the location
    /// <br/>NY/RY = add to the non/rotational Y component the delta Y to the location
    /// </summary>
    public static GenCtxProperty Target(RV2ControlMethod method, GCXF<Vector2> loc) => new TargetProp(method, loc, false);
    /// <summary>
    /// Same as TARGET, but targeting is performed from the summon location.
    /// </summary>
    public static GenCtxProperty SLTarget(RV2ControlMethod method, GCXF<Vector2> loc) => new TargetProp(method, loc, true);

    /// <summary>
    /// = Target Angle LPlayer
    /// </summary>
    public static GenCtxProperty Aimed() => Target(RV2ControlMethod.ANG, GCXF(_ => ExM.LPlayer()));
    /// <summary>
    /// Run the invocations only while a predicate is true. As long as the predicate is false,
    /// the function will wait indefinitely. If an `Unpause` command is used, then the unpause function
    /// will be run when this pauses and then unpauses.
    /// Not allowed for SyncPattern.
    /// </summary>
    /// <param name="pred"></param>
    /// <returns></returns>
    public static GenCtxProperty While(GCXF<bool> pred) => new WhileProp(pred);
    /// <summary>
    /// If using a While property, this causes code to be run when the function is paused and then unpaused.
    /// </summary>
    /// <param name="sm"></param>
    /// <returns></returns>
    public static GenCtxProperty Unpause(StateMachine sm) => new UnpauseProp(sm);
    /// <summary>
    /// Save some values into public hoisting for each fire. Resolved after PreLoop, right before invocation.
    /// </summary>
    /// <param name="targets"></param>
    /// <returns></returns>
    public static GenCtxProperty SaveF((ReflectEx.Hoist<float> target, GCXF<float> indexer, GCXF<float> valuer)[] targets) =>
        new SaveFProp(targets);
    /// <summary>
    /// Save some values into public hoisting for each fire. Resolved after PreLoop, right before invocation.
    /// </summary>
    /// <param name="targets"></param>
    /// <returns></returns>
    public static GenCtxProperty SaveV2((ReflectEx.Hoist<Vector2> target, GCXF<float> indexer, GCXF<Vector2> valuer)[] targets) =>
        new SaveV2Prop(targets);

    /// <summary>
    /// If a predicate is true, then do not execute this function. Resolved after start rules.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty Clip(GCXF<bool> clipIf) => new ClipProp(clipIf);
    
    /// <summary>
    /// If a predicate is true, then cancel execution. Resolved after preloop rules on every iteration.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty Cancel(GCXF<bool> cancelIf) => new CancelProp(cancelIf);

    /// <summary>
    /// Make a GCX variable available for use via private hoisting.
    /// You only need to use this when making variables available to external functions like bullet controls.
    /// If functions within the scope of GCX use a variable, it will be automatically exposed.
    /// </summary>
    public static GenCtxProperty Expose((Reflector.ExType, string)[] variables) => new ExposeProp(variables);

    /// <summary>
    /// Reset the summonTime variable (&amp;st) provided in GCX every iteration.
    /// </summary>
    public static GenCtxProperty TimeReset() => new TimeResetTag();

    /// <summary>
    /// Restarts the given timer for every iteration of the looper. Resolved before preloop rules.
    /// </summary>
    public static GenCtxProperty Timer(ETime.Timer timer) => new TimerProp(timer);
    
    /// <summary>
    /// Adds the location of the laser at the specified draw-time to the RV2 of the summoned entity.
    /// <br/>Note that this can only be used if the executing entity is a laser.
    /// </summary>
    /// <param name="indexer"></param>
    /// <returns></returns>
    public static GenCtxProperty OnLaser(GCXF<float> indexer) => new OnLaserProp(indexer);

    /// <summary>
    /// For a fire with a fixed RV2++, subtract the initial angle so that the bullets are
    /// evenly spread around the original angle.
    /// </summary>
    public static GenCtxProperty Center() => new CenterTag();
    
    /// <summary>
    /// Bind the values axd, ayd, aixd, aiyd in the GCX preloop section.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty BindArrow() => new BindArrowTag();
    /// <summary>
    /// Bind the values lr, rl in the GCX preloop section.
    /// </summary>
    /// <returns></returns>
    public static GenCtxProperty BindLR() => new BindLRTag();

    /// <summary>
    /// Bind a value corresponding to the loop number in the GCX preloop section.
    /// </summary>
    public static GenCtxProperty BindItr(string value) => new BindItrTag(value);

    public class CompositeProp : ValueProp<GenCtxProperty[]>, IUnrollable<GenCtxProperty> {
        public IEnumerable<GenCtxProperty> Values => value;
        public CompositeProp(params GenCtxProperty[] props) : base(props) { }
    }
    
    #region Impls

    public abstract class BPYProp : ValueProp<GCXF<float>> {
        public BPYProp(GCXF<float> f) : base(f) { } 
    }
    public abstract class PredProp : ValueProp<GCXF<bool>> {
        public PredProp(GCXF<bool> f) : base(f) { }
    }
    public abstract class TPProp : ValueProp<GCXF<Vector2>> {
        public TPProp(GCXF<Vector2> f) : base(f) { }
    }

    public class TimesProp : BPYProp {
        public readonly int? max;
        public TimesProp(int? max, GCXF<float> f) : base(f) => this.max = max;
    }
    public class WaitProp : BPYProp {
        public WaitProp(GCXF<float> f) : base(f) { }
    }
    public class ForTimeProp : BPYProp {
        public ForTimeProp(GCXF<float> f) : base(f) { }
    }
    public class DelayProp : BPYProp {
        public DelayProp(GCXF<float> f) : base(f) { }
    }
    public class FRV2Prop : ValueProp<GCXF<V2RV2>> {
        public FRV2Prop(GCXF<V2RV2> f) : base(f) { }
    }

    public class AlternateProp : BPYProp {
        public AlternateProp(GCXF<float> f) : base(f) { }
    }
    public class WhileProp : PredProp {
        public WhileProp(GCXF<bool> f) : base(f) { }
    }
    public class UnpauseProp : ValueProp<StateMachine> {
        public UnpauseProp(StateMachine f) : base(f) { }
    }

    public class ClipProp : ValueProp<GCXF<bool>> {
        public ClipProp(GCXF<bool> val) : base(val) { }
    }
    public class CancelProp : ValueProp<GCXF<bool>> {
        public CancelProp(GCXF<bool> val) : base(val) { }
    }

    public class TargetProp : TPProp {
        public readonly RV2ControlMethod method;
        public readonly bool fromSummon;
        public TargetProp(RV2ControlMethod method, GCXF<Vector2> func, bool fromSummon) : base(func) {
            this.method = method;
            this.fromSummon = fromSummon;
        }
    }

    public class SFXProp : ValueProp<string[]> {
        [CanBeNull] public readonly GCXF<float> indexer;
        [CanBeNull] public readonly GCXF<bool> pred;

        public SFXProp(string[] sfx, GCXF<float> indexer, GCXF<bool> pred) : base(sfx) {
            this.indexer = indexer;
            this.pred = pred;
        }
    }

    public class WaitChildProp : GenCtxProperty { }

    public class RootProp : TPProp {
        public readonly bool doAdjust;

        public RootProp(GCXF<Vector2> root, bool doAdjust) : base(root) {
            this.doAdjust = doAdjust;
        }
    }

    public class BankProp : GenCtxProperty {
        public readonly bool toZero;
        public readonly GCXF<V2RV2> banker;
        public BankProp(bool toZero, GCXF<V2RV2> f) {
            this.toZero = toZero;
            this.banker = f;
        }
        public BankProp(bool toZero, V2RV2 f) {
            this.toZero = toZero;
            this.banker = _ => f;
        }
    }

    public class ValueProp<T> : GenCtxProperty {
        public readonly T value;
        public ValueProp(T value) => this.value = value;
    }
    public class ParametrizationProp : ValueProp<Parametrization> {
        [CanBeNull] public readonly GCXF<float> mutater;

        public ParametrizationProp(Parametrization p, [CanBeNull] GCXF<float> mutater) : base(p) {
            this.mutater = mutater;
        }
    }

    public class ColorProp : ValueProp<string[]> {
        [CanBeNull] public readonly GCXF<float> indexer;
        public readonly bool reverse;

        public ColorProp(string[] colors, [CanBeNull] GCXF<float> indexer, bool reverse) : base(colors) {
            this.indexer = indexer;
            this.reverse = reverse;
        }
    }
    public abstract class RuleProp : ValueProp<GCRule> {
        public RuleProp(GCRule rule) : base(rule) { }
    }
    public abstract class RuleListProp : GenCtxProperty {
        public readonly List<GCRule> rules;
        public RuleListProp(GCRule[] rules) => this.rules = new List<GCRule>(rules);
    }

    public class PreLoopProp : RuleListProp {
        public PreLoopProp(GCRule[] rules) : base(rules) { }
    }

    public class PostLoopProp : RuleListProp {
        public PostLoopProp(GCRule[] rules) : base(rules) { }
    }
    public class StartProp : RuleListProp {
        public StartProp(GCRule[] rules) : base(rules) { }
    }
    public class EndProp : RuleListProp {
        public EndProp(GCRule[] rules) : base(rules) { }
    }

    /// <summary>
    /// RV2Incr X is the same as adding `rv2 += X` at the end of PostLoopProp.
    /// </summary>
    public class RV2IncrProp : ValueProp<GCXF<V2RV2>> {
        public RV2IncrProp(GCXF<V2RV2> rule) : base(rule) { }
        public RV2IncrProp(V2RV2 rv2) : this(_ => rv2) { }
    }

    public class MAngProp : BPYProp {
        public MAngProp(GCXF<float> mutater) : base(mutater) { }
    }

    public class SAHandlerProp : ValueProp<SummonAlongHandler> {
        public SAHandlerProp(SummonAlongHandler sah) : base(sah) { }
    }

    public class SaveFProp : GenCtxProperty {
        public readonly IReadOnlyList<(ReflectEx.Hoist<float> target, GCXF<float> indexer, GCXF<float> valuer)> targets;
        public SaveFProp((ReflectEx.Hoist<float>, GCXF<float>, GCXF<float>)[] targets) {
            this.targets = targets;
        }
    }
    public class SaveV2Prop : GenCtxProperty {
        public readonly IReadOnlyList<(ReflectEx.Hoist<Vector2> target, GCXF<float> indexer, GCXF<Vector2> valuer)> targets;
        public SaveV2Prop((ReflectEx.Hoist<Vector2>, GCXF<float>, GCXF<Vector2>)[] targets) {
            this.targets = targets;
        }
    }

    public class ExposeProp : ValueProp<(Reflector.ExType, string)[]> {
        public ExposeProp((Reflector.ExType, string)[] value) : base(value) { }
    }


    public class TimerProp : ValueProp<ETime.Timer> {
        public TimerProp(ETime.Timer t) : base(t) { } 
    }

    public class OnLaserProp : ValueProp<GCXF<float>> {
        public OnLaserProp(GCXF<float> f) : base(f) { }
    }

    public class RV2CircleTag : GenCtxProperty { }

    public class RV2SpreadProp : ValueProp<GCXF<V2RV2>> { 
        public RV2SpreadProp(GCXF<V2RV2> f) : base(f) { }
        
    }
    public class TimeResetTag : GenCtxProperty { }
    public class CenterTag : GenCtxProperty { }

    public class BindArrowTag : GenCtxProperty { }
    public class BindLRTag : GenCtxProperty { }

    public class BindItrTag : ValueProp<string> {
        public BindItrTag(string value): base(value) { }
    }

    #endregion

    public virtual int Priority => 0;
}

public static class GenCtxUtils {
    public static readonly GCXF<float> defltTimes = gcx => 1.0f;
    public static readonly GCXF<float> defltWait = gcx => 10.0f;
    public static readonly GCXF<float> zeroWait = gcx => 0.0f;
    public static readonly Type tSP = typeof(SyncPattern);
    public static readonly Type tAP = typeof(AsyncPattern);
    public static readonly Type tTP = typeof(StateMachine);
}
public class GenCtxProperties<T> {
    public readonly GCXF<float> times = defltTimes;
    public readonly int? maxTimes;
    public readonly GCXF<float> wait;
    [CanBeNull] public readonly GCXF<float> fortime;
    public readonly GCXF<float> delay = zeroWait;
    public readonly bool waitChild;
    [CanBeNull] public readonly GCXF<Vector2> forceRoot;
    public readonly bool forceRootAdjust;
    public readonly (bool, GCXF<V2RV2>)? bank;
    [CanBeNull] public readonly List<GCRule> preloop;
    public readonly List<GCRule> postloop = new List<GCRule>();
    [CanBeNull] public readonly List<GCRule> start;
    [CanBeNull] public readonly List<GCRule> end;
    public readonly Parametrization p = Enums.Parametrization.DEFER;
    [CanBeNull] public readonly GCXF<float> p_mutater;
    [CanBeNull] public readonly string[] colors;
    [CanBeNull] public readonly GCXF<float> colorsIndexer;
    public readonly bool colorsReverse;
    [CanBeNull] public readonly SummonAlongHandler sah;
    [CanBeNull] public readonly GCXF<V2RV2> frv2;
    public readonly Facing? facing;
    [CanBeNull] public readonly string[] sfx;
    [CanBeNull] public readonly GCXF<float> sfxIndexer;
    [CanBeNull] public readonly GCXF<bool> sfxIf;
    public readonly (RV2ControlMethod, GCXF<Vector2>)? target;
    public readonly bool targetFromSummon;
    [CanBeNull] public readonly GCXF<bool> runWhile;
    [CanBeNull] public readonly StateMachine unpause;
    [CanBeNull] public readonly IReadOnlyList<(ReflectEx.Hoist<float>, GCXF<float>, GCXF<float>)> saveF;
    [CanBeNull] public readonly IReadOnlyList<(ReflectEx.Hoist<Vector2>, GCXF<float>, GCXF<Vector2>)> saveV2;
    [CanBeNull] public readonly GCXF<float> childSelect;
    [CanBeNull] public readonly GCXF<bool> clipIf;
    [CanBeNull] public readonly GCXF<bool> cancelIf;
    [CanBeNull] public readonly (Reflector.ExType, string)[] expose;
    [CanBeNull] public readonly ETime.Timer timer;
    public readonly bool resetTime;
    public readonly bool centered;
    public readonly bool bindArrow;
    public readonly bool bindLR;
    [CanBeNull] public readonly string bindItr;
    [CanBeNull] public readonly GCXF<float> laserIndexer;
    private readonly RV2IncrType? rv2IncrType = null;
    [CanBeNull] private readonly GCXF<V2RV2> rv2Spread = null;
    private enum RV2IncrType {
        FUNC,
        CIRCLE,
        SPREAD
    }

    public V2RV2 PostloopRV2Incr(GenCtx gcx, int t) {
        if (rv2IncrType == RV2IncrType.FUNC) return rv2pp?.Invoke(gcx) ?? V2RV2.Zero;
        if (rv2IncrType == RV2IncrType.CIRCLE) return V2RV2.Angle(360f / t);
        else if (rv2IncrType == RV2IncrType.SPREAD) return 1f / (t-1) * (rv2Spread?.Invoke(gcx) ?? V2RV2.Zero);
        else return V2RV2.Zero;
    }
    
    
    [CanBeNull] public readonly GCXF<float> rv2aMutater;
    /// <summary>
    /// This is present to allow integration with other props, it is handled automatically via postloop.
    /// </summary>
    [CanBeNull] private readonly GCXF<V2RV2> rv2pp;

    /*public V2RV2 AnyRV2Increment(GenCtx gcx, int t) {
        if (specialIncrType != null) return SpecialRV2Incr(gcx, t);
        return rv2pp(gcx);
    }*/

    public GenCtxProperties(params GenCtxProperty[] props) : this(props as IEnumerable<GenCtxProperty>) { }

    public GenCtxProperties(IEnumerable<GenCtxProperty> props) {
        var t = typeof(T);
        bool allowWait = false;
        bool allowWaitChild = false;
        if (t == tSP) {
        } else if (t == tAP) {
            allowWait = true;
            allowWaitChild = true;
        } else if (t == tTP) {
            allowWait = true;
            allowWaitChild = true;
        } else throw new Reflector.StaticException($"Cannot call GenCtxProperties with class {t.RName()}");
        foreach (var prop in props.Unroll().OrderBy(x => x.Priority)) {
            if (prop is TimesProp gt) {
                maxTimes = gt.max ?? maxTimes;
                times = gt.value ?? times;
            } else if (prop is WaitProp wt && allowWait) wait = wt.value;
            else if (prop is ForTimeProp ftp && allowWait) fortime = ftp.value;
            else if (prop is DelayProp dp && allowWait) delay = dp.value;
            else if (prop is WaitChildProp && allowWaitChild) waitChild = true;
            else if (prop is RootProp rp) {
                forceRoot = rp.value;
                forceRootAdjust = rp.doAdjust;
            } else if (prop is BankProp bp) bank = (bp.toZero, bp.banker);
            else if (prop is PreLoopProp prel) prel.rules.AssignOrExtend(ref preloop);
            else if (prop is PostLoopProp pol) pol.rules.AssignOrExtend(ref postloop);
            else if (prop is RV2IncrProp rvp) {
                rv2IncrType = RV2IncrType.FUNC;
                rv2pp = rvp.value;
            } else if (prop is RV2CircleTag) rv2IncrType = RV2IncrType.CIRCLE;
            else if (prop is RV2SpreadProp rsp) {
                rv2IncrType = RV2IncrType.SPREAD;
                rv2Spread = rsp.value;
            } else if (prop is MAngProp map) rv2aMutater = map.value;
            else if (prop is FRV2Prop frv2p) frv2 = frv2p.value;
            else if (prop is StartProp sp) sp.rules.AssignOrExtend(ref start);
            else if (prop is EndProp ep) ep.rules.AssignOrExtend(ref end);
            else if (prop is ParametrizationProp pp) {
                p = pp.value;
                p_mutater = pp.mutater;
            } else if (prop is ColorProp cp) {
                colors = cp.value;
                colorsIndexer = cp.indexer;
                colorsReverse = cp.reverse;
            } else if (prop is SAHandlerProp sahp) sah = sahp.value;
            else if (prop is ValueProp<Facing> fp) facing = fp.value;
            else if (prop is SFXProp sfxp) {
                sfx = sfxp.value;
                sfxIf = sfxp.pred;
                sfxIndexer = sfxp.indexer;
            } else if (prop is TargetProp tp) {
                target = (tp.method, tp.value);
                targetFromSummon = tp.fromSummon;
            } else if (prop is WhileProp wp && allowWait) runWhile = wp.value;
            else if (prop is UnpauseProp up && allowWait) unpause = up.value;
            else if (prop is SaveFProp sfp) saveF = sfp.targets;
            else if (prop is SaveV2Prop sv2p) saveV2 = sv2p.targets;
            else if (prop is AlternateProp ap) childSelect = ap.value;
            else if (prop is ClipProp clipper) clipIf = clipper.value;
            else if (prop is CancelProp canceller) cancelIf = canceller.value;
            else if (prop is ExposeProp exp) expose = exp.value;
            else if (prop is TimeResetTag) resetTime = true;
            else if (prop is TimerProp trp) timer = trp.value;
            else if (prop is OnLaserProp olp) {
                laserIndexer = olp.value;
            } else if (prop is CenterTag) centered = true;
            else if (prop is BindArrowTag) bindArrow = true;
            else if (prop is BindLRTag) bindLR = true;
            else if (prop is BindItrTag bit) bindItr = bit.value;
            else throw new Exception($"{t.RName()} is not allowed to have properties of type {prop.GetType()}.");
        }
        if (sah != null) {
            if (frv2 != null) throw new Exception("A summon-along handler cannot be declared with an RV2 function handler.");
            rv2IncrType = null;
        }
        if (frv2 != null) rv2IncrType = null;
        if (wait == null) wait = (waitChild) ? zeroWait : defltWait;
        if (unpause != null && runWhile == null) throw new Exception("Unpause requires While to run. Unpause is only invoked when the While statement is set to false, and then set back to true.");
    }
}
}
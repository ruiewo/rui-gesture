namespace RuiGesture.Models.Core;

using System;
using System.Collections.Generic;
using System.Linq;

public class StateN<TConfig, TContextManager>
    : State<TConfig, TContextManager>
    where TConfig : GestureMachineConfig
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly GestureMachine<TConfig, TContextManager> Machine;

    public readonly EvaluationContext Ctx;
    public readonly History<TConfig, TContextManager> History;
    public readonly IReadOnlyList<IReadOnlyDoubleThrowElement> DoubleThrowElements;
    public readonly bool CanCancel;

    public readonly IDictionary<FireEvent, IReadOnlyList<IReadOnlySingleThrowElement>>
        InversedSingleThrowTrigger;

    public readonly IDictionary<PressEvent, IReadOnlyList<IReadOnlyDoubleThrowElement>>
        InversedDoubleThrowTrigger;

    public readonly IDictionary<StrokeSequence, IReadOnlyList<IReadOnlyStrokeElement>>
        InversedStrokeTrigger;

    public readonly IDictionary<PressEvent, IReadOnlyList<IReadOnlyDecomposedElement>>
        InversedDecomposedTrigger;

    public readonly IReadOnlyCollection<PhysicalReleaseEvent> EndTriggers;

    public bool IsEndTrigger(PhysicalReleaseEvent releaseEvent)
        => EndTriggers.Contains(releaseEvent);

    public readonly IReadOnlyCollection<PhysicalReleaseEvent> AbnormalEndTriggers;

    public bool IsAbnormalEndTrigger(PhysicalReleaseEvent releaseEvent)
        => AbnormalEndTriggers.Contains(releaseEvent);

    public StateN(
        GestureMachine<TConfig, TContextManager> machine,
        EvaluationContext ctx,
        History<TConfig, TContextManager> history,
        IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements,
        int depth,
        bool canCancel = true)
        : base(depth)
    {
        Machine = machine;
        Ctx = ctx;
        History = history;
        DoubleThrowElements = doubleThrowElements;
        CanCancel = canCancel;

        // Caches.
        InversedSingleThrowTrigger = GetInversedSingleThrowTrigger(DoubleThrowElements);
        InversedDoubleThrowTrigger = GetInversedDoubleThrowTrigger(DoubleThrowElements);
        InversedStrokeTrigger = GetInversedStrokeTrigger(DoubleThrowElements);
        InversedDecomposedTrigger = GetInversedDecomposedTrigger(DoubleThrowElements);
        EndTriggers = GetEndTriggers(History.Records);
        AbnormalEndTriggers = GetAbnormalEndTriggers(History.Records);
    }

    public override Result<TConfig, TContextManager> Input(IPhysicalEvent evnt)
    {
        if (evnt is PhysicalFireEvent fireEvent && IsSingleThrowTrigger(fireEvent))
        {
            var singleThrowElements = GetSingleThrowElementsByTrigger(fireEvent);
            Machine.ContextManager.ExecuteDoExecutors(Ctx, singleThrowElements);
            return Result.Create(true, ToNonCancellableClone());
        }
        else if (evnt is PhysicalPressEvent pressEvent)
        {
            if (IsRepeatedStartTrigger(pressEvent))
            {
                return Result.Create(true, this);
            }
            else if (IsDoubleThrowTrigger(pressEvent))
            {
                var nextDoubleThrowElements = GetDoubleThrowElementsByTrigger(pressEvent);
                if (HasPressExecutors(nextDoubleThrowElements) ||
                    HasReleaseExecutors(nextDoubleThrowElements))
                {
                    Machine.ContextManager.ExecutePressExecutors(Ctx, nextDoubleThrowElements);
                    return Result.Create(true,
                        CreateNonCancellableNextState(pressEvent, nextDoubleThrowElements));
                }
                else if (!CanCancel)
                {
                    return Result.Create(true,
                        CreateNonCancellableNextState(pressEvent, nextDoubleThrowElements));
                }

                return Result.Create(true,
                    CreateCancellableNextState(pressEvent, nextDoubleThrowElements));
            }
            else if (IsDecomposedTrigger(pressEvent))
            {
                var decomposedElements = GetDecomposedElementsByTrigger(pressEvent);
                Machine.ContextManager.ExecutePressExecutors(Ctx, decomposedElements);
                return Result.Create(true, ToNonCancellableClone());
            }
        }
        else if (evnt is PhysicalReleaseEvent releaseEvent)
        {
            var oppositeEvent = releaseEvent.Opposition;

            if (IsNormalEndTrigger(releaseEvent))
            {
                var strokeSequence = Machine.StrokeWatcher.GetStrokeSequence();
                if (strokeSequence.Any())
                {
                    if (IsStrokeTrigger(strokeSequence))
                    {
                        var strokeElements = GetStrokeElementsByTrigger(strokeSequence);
                        Machine.ContextManager.ExecuteDoExecutors(Ctx, strokeElements);
                    }

                    Machine.ContextManager.ExecuteReleaseExecutors(Ctx, DoubleThrowElements);
                }
                else if (HasPressExecutors || HasDoExecutors || HasReleaseExecutors)
                {
                    Machine.ContextManager.ExecuteDoExecutors(Ctx, DoubleThrowElements);
                    Machine.ContextManager.ExecuteReleaseExecutors(Ctx, DoubleThrowElements);
                }
                else if (CanCancel)
                {
                    Machine.CallbackManager.OnGestureCanceled(Machine, this);
                    return Result.Create(true, LastState);
                }

                if (LastState is StateN<TConfig, TContextManager> stateN)
                {
                    return Result.Create(true, stateN.ToNonCancellableClone());
                }

                return Result.Create(true, LastState);
            }
            else if (IsAbnormalEndTrigger(releaseEvent))
            {
                var queryResult = History.Query(releaseEvent);
                Machine.invalidEvents.IgnoreNext(queryResult.SkippedReleaseEvents);

                if (!CanCancel &&
                    queryResult.FoundState is StateN<TConfig, TContextManager> stateN)
                {
                    return Result.Create(true, stateN.ToNonCancellableClone());
                }

                return Result.Create(true, queryResult.FoundState);
            }
            else if (IsDecomposedTrigger(oppositeEvent))
            {
                var decomposedElements = GetDecomposedElementsByTrigger(oppositeEvent);
                Machine.ContextManager.ExecuteReleaseExecutors(Ctx, decomposedElements);
                return Result.Create(true, ToNonCancellableClone());
            }
        }

        return base.Input(evnt);
    }

    public override State<TConfig, TContextManager> Timeout()
    {
        if (CanCancel &&
            !HasPressExecutors && !HasDoExecutors && !HasReleaseExecutors &&
            !Machine.StrokeWatcher.StrokeIsEstablished)
        {
            return LastState;
        }

        return this;
    }

    public override State<TConfig, TContextManager> Reset()
    {
        Machine.invalidEvents.IgnoreNext(NormalEndTrigger);
        Machine.ContextManager.ExecuteReleaseExecutors(Ctx, DoubleThrowElements);
        return LastState;
    }

    private StateN<TConfig, TContextManager> ToNonCancellableClone()
        => new(
            Machine, Ctx, History, DoubleThrowElements, Depth, false);

    private StateN<TConfig, TContextManager> CreateCancellableNextState(
        PhysicalPressEvent pressEvent,
        IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => new(
            Machine, Ctx, History.CreateNext(pressEvent.Opposition, this),
            doubleThrowElements, Depth + 1, true);

    private StateN<TConfig, TContextManager> CreateNonCancellableNextState(
        PhysicalPressEvent pressEvent,
        IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => new(
            Machine, Ctx, History.CreateNext(pressEvent.Opposition, ToNonCancellableClone()),
            doubleThrowElements, Depth + 1, false);

    public bool IsRepeatedStartTrigger(PhysicalPressEvent pressEvent)
        => EndTriggers.Contains(pressEvent.Opposition);

    public PhysicalReleaseEvent NormalEndTrigger => History.Records.Last().ReleaseEvent;

    public bool IsNormalEndTrigger(PhysicalReleaseEvent releaseEvent)
        => NormalEndTrigger == releaseEvent;

    public State<TConfig, TContextManager> LastState => History.Records.Last().State;

    public new bool HasPressExecutors => HasPressExecutors(DoubleThrowElements);

    public new bool HasDoExecutors => HasDoExecutors(DoubleThrowElements);

    public new bool HasReleaseExecutors => HasReleaseExecutors(DoubleThrowElements);

    public IReadOnlyList<HistoryRecord<TConfig, TContextManager>>
        CreateHistory(
            IReadOnlyList<HistoryRecord<TConfig, TContextManager>> history,
            PhysicalPressEvent pressEvent,
            State<TConfig, TContextManager> state)
    {
        var newHistory = history.ToList();
        newHistory.Add(HistoryRecord.Create(pressEvent.Opposition, state));
        return newHistory;
    }

    private static IReadOnlyCollection<PhysicalReleaseEvent>
        GetEndTriggers(IReadOnlyList<HistoryRecord<TConfig, TContextManager>> history)
        => new HashSet<PhysicalReleaseEvent>(from h in history select h.ReleaseEvent);

    private static IReadOnlyCollection<PhysicalReleaseEvent>
        GetAbnormalEndTriggers(
            IReadOnlyList<HistoryRecord<TConfig, TContextManager>> history)
        => new HashSet<PhysicalReleaseEvent>(from h in history.Reverse().Skip(1) select h.ReleaseEvent);

    public bool IsSingleThrowTrigger(PhysicalFireEvent fireEvent)
        => InversedSingleThrowTrigger.Keys.Contains(fireEvent) ||
           InversedSingleThrowTrigger.Keys.Contains(fireEvent.LogicalNormalized);

    public bool IsDoubleThrowTrigger(PhysicalPressEvent pressEvent)
        => InversedDoubleThrowTrigger.Keys.Contains(pressEvent) ||
           InversedDoubleThrowTrigger.Keys.Contains(pressEvent.LogicalNormalized);

    public bool IsDecomposedTrigger(PhysicalPressEvent pressEvent)
        => InversedDecomposedTrigger.Keys.Contains(pressEvent) ||
           InversedDecomposedTrigger.Keys.Contains(pressEvent.LogicalNormalized);

    public bool IsStrokeTrigger(StrokeSequence strokes)
        => InversedStrokeTrigger.Keys.Contains(strokes);

    public IReadOnlyList<IReadOnlySingleThrowElement>
        GetSingleThrowElementsByTrigger(PhysicalFireEvent fireEvent)
    {
        if (InversedSingleThrowTrigger.TryGetValue(fireEvent, out var doubleThrowElements))
        {
            return doubleThrowElements;
        }

        return InversedSingleThrowTrigger[fireEvent.LogicalNormalized];
    }

    public IReadOnlyList<IReadOnlyDoubleThrowElement>
        GetDoubleThrowElementsByTrigger(PhysicalPressEvent pressEvent)
    {
        if (InversedDoubleThrowTrigger.TryGetValue(pressEvent, out var doubleThrowElements))
        {
            return doubleThrowElements;
        }

        return InversedDoubleThrowTrigger[pressEvent.LogicalNormalized];
    }

    public IReadOnlyList<IReadOnlyStrokeElement>
        GetStrokeElementsByTrigger(StrokeSequence strokeSequence)
        => InversedStrokeTrigger[strokeSequence];

    public IReadOnlyList<IReadOnlyDecomposedElement>
        GetDecomposedElementsByTrigger(PhysicalPressEvent pressEvent)
    {
        if (InversedDecomposedTrigger.TryGetValue(pressEvent, out var doubleThrowElements))
        {
            return doubleThrowElements;
        }

        return InversedDecomposedTrigger[pressEvent.LogicalNormalized];
    }

    private static IDictionary<FireEvent, IReadOnlyList<IReadOnlySingleThrowElement>>
        GetInversedSingleThrowTrigger(
            IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => (from d in doubleThrowElements
                where d.IsFull
                select from s in d.SingleThrowElements
                    where s.IsFull
                    select Tuple.Create(s, s.Trigger))
            .Aggregate(new List<Tuple<IReadOnlySingleThrowElement, FireEvent>>(), (a, b) =>
            {
                a.AddRange(b);
                return a;
            })
            .ToLookup(t => t.Item2, t => t.Item1)
            .ToDictionary(x => x.Key,
                x => x.Distinct().ToList() as IReadOnlyList<IReadOnlySingleThrowElement>);

    private static IDictionary<PressEvent, IReadOnlyList<IReadOnlyDoubleThrowElement>>
        GetInversedDoubleThrowTrigger(
            IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => (from d in doubleThrowElements
                where d.IsFull
                select from dd in d.DoubleThrowElements
                    where dd.IsFull
                    select Tuple.Create(dd, dd.Trigger))
            .Aggregate(new List<Tuple<IReadOnlyDoubleThrowElement, PressEvent>>(), (a, b) =>
            {
                a.AddRange(b);
                return a;
            })
            .ToLookup(t => t.Item2, t => t.Item1)
            .ToDictionary(x => x.Key,
                x => x.Distinct().ToList() as IReadOnlyList<IReadOnlyDoubleThrowElement>);

    private static IDictionary<StrokeSequence, IReadOnlyList<IReadOnlyStrokeElement>>
        GetInversedStrokeTrigger(
            IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => (from d in doubleThrowElements
                where d.IsFull
                select from s in d.StrokeElements
                    where s.IsFull
                    select Tuple.Create(s, s.Strokes))
            .Aggregate(new List<Tuple<IReadOnlyStrokeElement, StrokeSequence>>(), (a, b) =>
            {
                a.AddRange(b);
                return a;
            })
            .ToLookup(t => t.Item2, t => t.Item1)
            .ToDictionary(x => x.Key,
                x => x.Distinct().ToList() as IReadOnlyList<IReadOnlyStrokeElement>);

    private static IDictionary<PressEvent, IReadOnlyList<IReadOnlyDecomposedElement>>
        GetInversedDecomposedTrigger(
            IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => (from d in doubleThrowElements
                where d.IsFull
                select from dd in d.DecomposedElements
                    where dd.IsFull
                    select Tuple.Create(dd, dd.Trigger))
            .Aggregate(new List<Tuple<IReadOnlyDecomposedElement, PressEvent>>(), (a, b) =>
            {
                a.AddRange(b);
                return a;
            })
            .ToLookup(t => t.Item2, t => t.Item1)
            .ToDictionary(x => x.Key,
                x => x.Distinct().ToList() as IReadOnlyList<IReadOnlyDecomposedElement>);

    public override string ToString()
        => $"StateN(" +
           $"Depth: {Depth}, " +
           $"CanCancel: {CanCancel}, " +
           $"SingleThrowTriggers: [{string.Join(", ", InversedSingleThrowTrigger.Keys.Select(k => k.LogicalKey))}], " +
           $"DoubleThrowTriggers: [{string.Join(", ", InversedDoubleThrowTrigger.Keys.Select(k => k.LogicalKey))}], " +
           $"StrokeTriggers: [{string.Join(", ", InversedStrokeTrigger.Keys)}], " +
           $"DecomposedTriggers: [{string.Join(", ", InversedDecomposedTrigger.Keys.Select(k => k.LogicalKey))}], " +
           $"EndTriggers: [{string.Join(", ", History.Records.Select(r => r.ReleaseEvent.LogicalKey))}])";
}
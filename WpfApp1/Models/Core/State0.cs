namespace RuiGesture.Models.Core;

using System;
using System.Collections.Generic;
using System.Linq;

public class State0<TContextManager> : State<TContextManager>
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly GestureMachine<TContextManager> Machine;
    public readonly IReadOnlyRootElement RootElement;

    public readonly IDictionary<FireEvent, IReadOnlyList<IReadOnlyWhenElement>>
        InversedSingleThrowTrigger;

    public readonly IDictionary<PressEvent, IReadOnlyList<IReadOnlyWhenElement>>
        InversedDoubleThrowTrigger;

    public readonly IDictionary<PressEvent, IReadOnlyList<IReadOnlyWhenElement>>
        InversedDecomposedTrigger;

    public State0(
        GestureMachine<TContextManager> machine,
        IReadOnlyRootElement rootElement)
        : base(0)
    {
        Machine = machine;
        RootElement = rootElement;

        // Caches.
        InversedSingleThrowTrigger = GetInversedSingleThrowTrigger(RootElement);
        InversedDoubleThrowTrigger = GetInversedDoubleThrowTrigger(RootElement);
        InversedDecomposedTrigger = GetInversedDecomposedTrigger(RootElement);
    }

    public override Result<TContextManager> Input(IPhysicalEvent evnt)
    {
        if (evnt is PhysicalFireEvent fireEvent && IsSingleThrowTrigger(fireEvent))
        {
            var ctx = Machine.ContextManager.CreateEvaluateContext();
            var whenElements = FilterActiveWhenElements(ctx, GetWhenElementsBySingleThrowTrigger(fireEvent));
            var singleThrowElements = GetSingleThrowElements(whenElements, fireEvent);
            if (singleThrowElements.Any())
            {
                Machine.ContextManager.ExecuteDoExecutors(ctx, singleThrowElements);
                return Result.Create(true, this);
            }
        }

        else if (evnt is PhysicalPressEvent pressEvent)
        {
            if (IsDoubleThrowTrigger(pressEvent))
            {
                var ctx = Machine.ContextManager.CreateEvaluateContext();
                var whenElements = FilterActiveWhenElements(ctx, GetWhenElementsByDoubleThrowTrigger(pressEvent));
                var doubleThrowElements = GetDoubleThrowElements(whenElements, pressEvent);
                if (doubleThrowElements.Any())
                {
                    Machine.ContextManager.ExecutePressExecutors(ctx, doubleThrowElements);
                    var nextState = new StateN<TContextManager>(
                        Machine,
                        ctx,
                        History.Create(pressEvent.Opposition, this),
                        doubleThrowElements,
                        Depth + 1,
                        true);
                    return Result.Create(true, nextState);
                }
            }
            else if (IsDecomposedTrigger(pressEvent))
            {
                var ctx = Machine.ContextManager.CreateEvaluateContext();
                var whenElements = FilterActiveWhenElements(ctx, GetWhenElementsByDecomposedTrigger(pressEvent));
                var decomposedElements = GetDecomposedElements(whenElements, pressEvent);
                if (decomposedElements.Any())
                {
                    Machine.ContextManager.ExecutePressExecutors(ctx, decomposedElements);
                    return Result.Create(true, this);
                }
            }
        }
        else if (evnt is PhysicalReleaseEvent releaseEvent)
        {
            var oppositeEvent = releaseEvent.Opposition;

            if (IsDoubleThrowTrigger(oppositeEvent) || IsDecomposedTrigger(oppositeEvent))
            {
                var ctx = Machine.ContextManager.CreateEvaluateContext();
                var whenElements = FilterActiveWhenElements(ctx, GetWhenElementsByDecomposedTrigger(oppositeEvent));
                var decomposedElements = GetDecomposedElements(whenElements, oppositeEvent);
                if (decomposedElements.Any())
                {
                    Machine.ContextManager.ExecuteReleaseExecutors(ctx, decomposedElements);
                    return Result.Create(true, this);
                }
            }
        }

        return base.Input(evnt);
    }

    public bool IsSingleThrowTrigger(PhysicalFireEvent fireEvent)
        => InversedSingleThrowTrigger.Keys.Contains(fireEvent) ||
           InversedSingleThrowTrigger.Keys.Contains(fireEvent.LogicalNormalized);

    public bool IsDoubleThrowTrigger(PhysicalPressEvent pressEvent)
        => InversedDoubleThrowTrigger.Keys.Contains(pressEvent) ||
           InversedDoubleThrowTrigger.Keys.Contains(pressEvent.LogicalNormalized);

    public bool IsDecomposedTrigger(PhysicalPressEvent pressEvent)
        => InversedDecomposedTrigger.Keys.Contains(pressEvent) ||
           InversedDecomposedTrigger.Keys.Contains(pressEvent.LogicalNormalized);

    // todo: ContextManagerに移譲
    // CtxがCancellarationTokenを持つように
    // これは仕様には含まず、Appの拡張でいい
    //
    // ConfigureAwait(false)
    //
    // CancellarationTOkenをWhenのタイムアウトとDoの全体に
    public IReadOnlyList<IReadOnlyWhenElement>
        FilterActiveWhenElements(
            EvaluationContext ctx,
            IEnumerable<IReadOnlyWhenElement> whenElements)
        => Machine.ContextManager.Evaluate(ctx, whenElements);

    public IReadOnlyList<IReadOnlyWhenElement>
        GetWhenElementsBySingleThrowTrigger(PhysicalFireEvent fireEvent)
    {
        if (InversedSingleThrowTrigger.TryGetValue(fireEvent, out var a))
        {
            return a;
        }
        else if (InversedSingleThrowTrigger.TryGetValue(fireEvent.LogicalNormalized, out var b))
        {
            return b;
        }

        return new List<IReadOnlyWhenElement>();
    }

    public IReadOnlyList<IReadOnlyWhenElement>
        GetWhenElementsByDoubleThrowTrigger(PhysicalPressEvent pressEvent)
    {
        if (InversedDoubleThrowTrigger.TryGetValue(pressEvent, out var a))
        {
            return a;
        }
        else if (InversedDoubleThrowTrigger.TryGetValue(pressEvent.LogicalNormalized, out var b))
        {
            return b;
        }

        return new List<IReadOnlyWhenElement>();
    }

    public IReadOnlyList<IReadOnlyWhenElement>
        GetWhenElementsByDecomposedTrigger(PhysicalPressEvent pressEvent)
    {
        if (InversedDecomposedTrigger.TryGetValue(pressEvent, out var a))
        {
            return a;
        }
        else if (InversedDecomposedTrigger.TryGetValue(pressEvent.LogicalNormalized, out var b))
        {
            return b;
        }

        return new List<IReadOnlyWhenElement>();
    }

    protected internal IReadOnlyList<IReadOnlyDoubleThrowElement>
        GetDoubleThrowElements(
            IEnumerable<IReadOnlyWhenElement> whenElements,
            PhysicalPressEvent triggerEvent)
        => (from w in whenElements
                select from d in w.DoubleThrowElements
                    where d.IsFull && (d.Trigger.Equals(triggerEvent) ||
                                       d.Trigger.Equals(triggerEvent.LogicalNormalized))
                    select d)
            .Aggregate(new List<IReadOnlyDoubleThrowElement>(), (a, b) =>
            {
                a.AddRange(b);
                return a;
            });

    protected internal IReadOnlyList<IReadOnlyDecomposedElement>
        GetDecomposedElements(
            IEnumerable<IReadOnlyWhenElement> whenElements,
            PhysicalPressEvent triggerEvent)
        => (from w in whenElements
                select from d in w.DecomposedElements
                    where d.IsFull && (d.Trigger.Equals(triggerEvent) ||
                                       d.Trigger.Equals(triggerEvent.LogicalNormalized))
                    select d)
            .Aggregate(new List<IReadOnlyDecomposedElement>(), (a, b) =>
            {
                a.AddRange(b);
                return a;
            });

    protected internal IReadOnlyList<IReadOnlySingleThrowElement>
        GetSingleThrowElements(
            IEnumerable<IReadOnlyWhenElement> whenElements,
            PhysicalFireEvent triggerEvent)
        => (from w in whenElements
                select from s in w.SingleThrowElements
                    where s.IsFull && (s.Trigger.Equals(triggerEvent) ||
                                       s.Trigger.Equals(triggerEvent.LogicalNormalized))
                    select s)
            .Aggregate(new List<IReadOnlySingleThrowElement>(), (a, b) =>
            {
                a.AddRange(b);
                return a;
            });

    internal static IDictionary<FireEvent, IReadOnlyList<IReadOnlyWhenElement>>
        GetInversedSingleThrowTrigger(IReadOnlyRootElement rootElement)
        => (from w in rootElement.WhenElements
                select from s in w.SingleThrowElements
                    where s.IsFull
                    select Tuple.Create(w, s.Trigger))
            .Aggregate(new List<Tuple<IReadOnlyWhenElement, FireEvent>>(),
                (a, b) =>
                {
                    a.AddRange(b);
                    return a;
                })
            .ToLookup(t => t.Item2, t => t.Item1)
            .ToDictionary(x => x.Key,
                x => x.Distinct().ToList() as IReadOnlyList<IReadOnlyWhenElement>);

    internal static IDictionary<PressEvent, IReadOnlyList<IReadOnlyWhenElement>>
        GetInversedDoubleThrowTrigger(IReadOnlyRootElement rootElement)
        => (from w in rootElement.WhenElements
                select from d in w.DoubleThrowElements
                    where d.IsFull
                    select Tuple.Create(w, d.Trigger))
            .Aggregate(new List<Tuple<IReadOnlyWhenElement, PressEvent>>(),
                (a, b) =>
                {
                    a.AddRange(b);
                    return a;
                })
            .ToLookup(t => t.Item2, t => t.Item1)
            .ToDictionary(x => x.Key,
                x => x.Distinct().ToList() as IReadOnlyList<IReadOnlyWhenElement>);

    internal static IDictionary<PressEvent, IReadOnlyList<IReadOnlyWhenElement>>
        GetInversedDecomposedTrigger(IReadOnlyRootElement rootElement)
        => (from w in rootElement.WhenElements
                select from d in w.DecomposedElements
                    where d.IsFull
                    select Tuple.Create(w, d.Trigger))
            .Aggregate(new List<Tuple<IReadOnlyWhenElement, PressEvent>>(),
                (a, b) =>
                {
                    a.AddRange(b);
                    return a;
                })
            .ToLookup(t => t.Item2, t => t.Item1)
            .ToDictionary(x => x.Key,
                x => x.Distinct().ToList() as IReadOnlyList<IReadOnlyWhenElement>);

    public override string ToString()
        => $"State0(" +
           $"Depth: {Depth}, " +
           $"SingleThrowTriggers: [{string.Join(", ", InversedSingleThrowTrigger.Keys.Select(k => k.LogicalKey))}], " +
           $"DoubleThrowTriggers: [{string.Join(", ", InversedDoubleThrowTrigger.Keys.Select(k => k.LogicalKey))}], " +
           $"DecomposedTriggers: [{string.Join(", ", InversedDecomposedTrigger.Keys.Select(k => k.LogicalKey))}])";
}
namespace RuiGesture.Models.Core;

using System;
using System.Collections.Generic;
using System.Linq;

public interface IReadOnlyElement
{
    bool IsFull { get; }
    int GestureCount { get; }
}

public abstract class Element : IReadOnlyElement
{
    public abstract bool IsFull { get; }
    public abstract int GestureCount { get; }
}

public interface IReadOnlyRootElement : IReadOnlyElement
{
    IReadOnlyList<IReadOnlyWhenElement> WhenElements { get; }
}

public class RootElement : Element, IReadOnlyRootElement
{
    public override bool IsFull => WhenElements.Any(e => e.IsFull);

    public override int GestureCount => WhenElements.Sum(e => e.GestureCount);

    private readonly List<WhenElement> whenElements = new();
    public IReadOnlyList<IReadOnlyWhenElement> WhenElements => whenElements;

    public WhenElement When(EvaluateAction evaluator)
    {
        var elm = new WhenElement(evaluator);
        whenElements.Add(elm);
        return elm;
    }
}

public interface IReadOnlyWhenElement : IReadOnlyElement
{
    EvaluateAction WhenEvaluator { get; }
    IReadOnlyList<IReadOnlySingleThrowElement> SingleThrowElements { get; }
    IReadOnlyList<IReadOnlyDoubleThrowElement> DoubleThrowElements { get; }
    IReadOnlyList<IReadOnlyDecomposedElement> DecomposedElements { get; }
}

public class WhenElement : Element, IReadOnlyWhenElement
{
    public override bool IsFull
        => WhenEvaluator != null &&
           (SingleThrowElements.Any(e => e.IsFull) ||
            DoubleThrowElements.Any(e => e.IsFull) ||
            DecomposedElements.Any(e => e.IsFull));

    public override int GestureCount
        => SingleThrowElements.Sum(e => e.GestureCount) +
           DoubleThrowElements.Sum(e => e.GestureCount) +
           DecomposedElements.Sum(e => e.GestureCount);

    public EvaluateAction WhenEvaluator { get; }

    private readonly List<SingleThrowElement> singleThrowElements = new();
    public IReadOnlyList<IReadOnlySingleThrowElement> SingleThrowElements => singleThrowElements;

    private readonly List<DoubleThrowElement> doubleThrowElements = new();
    public IReadOnlyList<IReadOnlyDoubleThrowElement> DoubleThrowElements => doubleThrowElements;

    private readonly List<DecomposedElement> decomposedElements = new();
    public IReadOnlyList<IReadOnlyDecomposedElement> DecomposedElements => decomposedElements;

    public WhenElement(EvaluateAction evaluator)
    {
        WhenEvaluator = evaluator;
    }

    public SingleThrowElement On(LogicalSingleThrowKey singleThrowKey)
    {
        var elm = new SingleThrowElement(singleThrowKey.FireEvent as FireEvent);
        singleThrowElements.Add(elm);
        return elm;
    }

    public SingleThrowElement On(PhysicalSingleThrowKey singleThrowKey)
    {
        var elm = new SingleThrowElement(singleThrowKey.FireEvent as FireEvent);
        singleThrowElements.Add(elm);
        return elm;
    }

    public DoubleThrowElement On(LogicalDoubleThrowKey doubleThrowKey)
    {
        if (DecomposedElements.Any())
        {
            throw new InvalidOperationException("Declaration is ambiguous; " +
                                                "DoubleThrowKey can not be used with `On()` and `OnDecomposed()` on the same context.");
        }

        var elm = new DoubleThrowElement(doubleThrowKey.PressEvent as PressEvent);
        doubleThrowElements.Add(elm);
        return elm;
    }

    public DoubleThrowElement On(PhysicalDoubleThrowKey doubleThrowKey)
    {
        if (DecomposedElements.Any())
        {
            throw new InvalidOperationException("Declaration is ambiguous; " +
                                                "DoubleThrowKey can not be used with `On()` and `OnDecomposed()` on the same context.");
        }

        var elm = new DoubleThrowElement(doubleThrowKey.PressEvent as PressEvent);
        doubleThrowElements.Add(elm);
        return elm;
    }

    public DecomposedElement OnDecomposed(LogicalDoubleThrowKey doubleThrowKey)
    {
        if (DoubleThrowElements.Any())
        {
            throw new InvalidOperationException("Declaration is ambiguous; " +
                                                "DoubleThrowKey can not be used with `On()` and `OnDecomposed()` on the same context.");
        }

        var elm = new DecomposedElement(doubleThrowKey.PressEvent as PressEvent);
        decomposedElements.Add(elm);
        return elm;
    }

    public DecomposedElement OnDecomposed(PhysicalDoubleThrowKey doubleThrowKey)
    {
        if (DoubleThrowElements.Any())
        {
            throw new InvalidOperationException("Declaration is ambiguous; " +
                                                "DoubleThrowKey can not be used with `On()` and `OnDecomposed()` on the same context.");
        }

        var elm = new DecomposedElement(doubleThrowKey.PressEvent as PressEvent);
        decomposedElements.Add(elm);
        return elm;
    }
}

public interface IReadOnlySingleThrowElement : IReadOnlyElement
{
    FireEvent Trigger { get; }
    IReadOnlyList<ExecuteAction> DoExecutors { get; }
}

public class SingleThrowElement : Element, IReadOnlySingleThrowElement
{
    public override bool IsFull => Trigger != null && DoExecutors.Any(e => e != null);

    public override int GestureCount => DoExecutors.Count;

    public FireEvent Trigger { get; }

    private readonly List<ExecuteAction> doExecutors = new();
    public IReadOnlyList<ExecuteAction> DoExecutors => doExecutors;

    public SingleThrowElement(FireEvent fireEvent)
    {
        Trigger = fireEvent;
    }

    public SingleThrowElement Do(ExecuteAction executor)
    {
        doExecutors.Add(executor);
        return this;
    }
}

public interface IReadOnlyDoubleThrowElement : IReadOnlyElement
{
    PressEvent Trigger { get; }
    IReadOnlyList<IReadOnlySingleThrowElement> SingleThrowElements { get; }
    IReadOnlyList<IReadOnlyDoubleThrowElement> DoubleThrowElements { get; }
    IReadOnlyList<IReadOnlyDecomposedElement> DecomposedElements { get; }
    IReadOnlyList<IReadOnlyStrokeElement> StrokeElements { get; }
    IReadOnlyList<ExecuteAction> PressExecutors { get; }
    IReadOnlyList<ExecuteAction> DoExecutors { get; }
    IReadOnlyList<ExecuteAction> ReleaseExecutors { get; }
}

public class DoubleThrowElement : Element, IReadOnlyDoubleThrowElement
{
    public override bool IsFull
        => Trigger != null &&
           (PressExecutors.Any(e => e != null) ||
            DoExecutors.Any(e => e != null) ||
            ReleaseExecutors.Any(e => e != null) ||
            SingleThrowElements.Any(e => e.IsFull) ||
            DoubleThrowElements.Any(e => e.IsFull) ||
            DecomposedElements.Any(e => e.IsFull) ||
            StrokeElements.Any(e => e.IsFull));

    public override int GestureCount
        => SingleThrowElements.Sum(e => e.GestureCount) +
           DoubleThrowElements.Sum(e => e.GestureCount) +
           DecomposedElements.Sum(e => e.GestureCount) +
           StrokeElements.Sum(e => e.GestureCount) +
           PressExecutors.Count +
           DoExecutors.Count +
           ReleaseExecutors.Count;

    public PressEvent Trigger { get; }

    private readonly List<SingleThrowElement> singleThrowElements = new();
    public IReadOnlyList<IReadOnlySingleThrowElement> SingleThrowElements => singleThrowElements;

    private readonly List<DoubleThrowElement> doubleThrowElements = new();
    public IReadOnlyList<IReadOnlyDoubleThrowElement> DoubleThrowElements => doubleThrowElements;

    private readonly List<DecomposedElement> decomposedElements = new();
    public IReadOnlyList<IReadOnlyDecomposedElement> DecomposedElements => decomposedElements;

    private readonly List<StrokeElement> strokeElements = new();
    public IReadOnlyList<IReadOnlyStrokeElement> StrokeElements => strokeElements;

    private readonly List<ExecuteAction> pressExecutors = new();
    public IReadOnlyList<ExecuteAction> PressExecutors => pressExecutors;

    private readonly List<ExecuteAction> doExecutors = new();
    public IReadOnlyList<ExecuteAction> DoExecutors => doExecutors;

    private readonly List<ExecuteAction> releaseExecutors = new();
    public IReadOnlyList<ExecuteAction> ReleaseExecutors => releaseExecutors;

    public DoubleThrowElement(PressEvent pressEvent)
    {
        Trigger = pressEvent;
    }

    public SingleThrowElement On(LogicalSingleThrowKey singleThrowKey)
    {
        var elm = new SingleThrowElement(singleThrowKey.FireEvent as FireEvent);
        singleThrowElements.Add(elm);
        return elm;
    }

    public SingleThrowElement On(PhysicalSingleThrowKey singleThrowKey)
    {
        var elm = new SingleThrowElement(singleThrowKey.FireEvent as FireEvent);
        singleThrowElements.Add(elm);
        return elm;
    }

    public DoubleThrowElement On(LogicalDoubleThrowKey doubleThrowKey)
    {
        if (DecomposedElements.Any())
        {
            throw new InvalidOperationException("Declaration is ambiguous; " +
                                                "DoubleThrowKey can not be used with `On()` and `OnDecomposed()` on the same context.");
        }

        var elm = new DoubleThrowElement(doubleThrowKey.PressEvent as PressEvent);
        doubleThrowElements.Add(elm);
        return elm;
    }

    public DoubleThrowElement On(PhysicalDoubleThrowKey doubleThrowKey)
    {
        if (DecomposedElements.Any())
        {
            throw new InvalidOperationException("Declaration is ambiguous; " +
                                                "DoubleThrowKey can not be used with `On()` and `OnDecomposed()` on the same context.");
        }

        var elm = new DoubleThrowElement(doubleThrowKey.PressEvent as PressEvent);
        doubleThrowElements.Add(elm);
        return elm;
    }

    public DecomposedElement OnDecomposed(LogicalDoubleThrowKey doubleThrowKey)
    {
        if (DoubleThrowElements.Any())
        {
            throw new InvalidOperationException("Declaration is ambiguous; " +
                                                "DoubleThrowKey can not be used with `On()` and `OnDecomposed()` on the same context.");
        }

        var elm = new DecomposedElement(doubleThrowKey.PressEvent as PressEvent);
        decomposedElements.Add(elm);
        return elm;
    }

    public DecomposedElement OnDecomposed(PhysicalDoubleThrowKey doubleThrowKey)
    {
        if (DoubleThrowElements.Any())
        {
            throw new InvalidOperationException("Declaration is ambiguous; " +
                                                "DoubleThrowKey can not be used with `On()` and `OnDecomposed()` on the same context.");
        }

        var elm = new DecomposedElement(doubleThrowKey.PressEvent as PressEvent);
        decomposedElements.Add(elm);
        return elm;
    }

    public StrokeElement On(params StrokeDirection[] strokeDirections)
    {
        var elm = new StrokeElement(strokeDirections);
        strokeElements.Add(elm);
        return elm;
    }

    public DoubleThrowElement Press(ExecuteAction executor)
    {
        pressExecutors.Add(executor);
        return this;
    }

    public DoubleThrowElement Do(ExecuteAction executor)
    {
        doExecutors.Add(executor);
        return this;
    }

    public DoubleThrowElement Release(ExecuteAction executor)
    {
        releaseExecutors.Add(executor);
        return this;
    }
}

public interface IReadOnlyDecomposedElement : IReadOnlyElement
{
    PressEvent Trigger { get; }
    IReadOnlyList<ExecuteAction> PressExecutors { get; }
    IReadOnlyList<ExecuteAction> ReleaseExecutors { get; }
}

public class DecomposedElement : Element, IReadOnlyDecomposedElement
{
    public override bool IsFull
        => Trigger != null &&
           (PressExecutors.Any(e => e != null) ||
            ReleaseExecutors.Any(e => e != null));

    public override int GestureCount
        => PressExecutors.Count + ReleaseExecutors.Count;

    public PressEvent Trigger { get; }

    private readonly List<ExecuteAction> pressExecutors = new();
    public IReadOnlyList<ExecuteAction> PressExecutors => pressExecutors;

    private readonly List<ExecuteAction> releaseExecutors = new();
    public IReadOnlyList<ExecuteAction> ReleaseExecutors => releaseExecutors;

    public DecomposedElement(PressEvent pressEvent)
    {
        Trigger = pressEvent;
    }

    public DecomposedElement Press(ExecuteAction executor)
    {
        pressExecutors.Add(executor);
        return this;
    }

    public DecomposedElement Release(ExecuteAction executor)
    {
        releaseExecutors.Add(executor);
        return this;
    }
}

public interface IReadOnlyStrokeElement : IReadOnlyElement
{
    StrokeSequence Strokes { get; }
    IReadOnlyList<ExecuteAction> DoExecutors { get; }
}

public class StrokeElement : Element, IReadOnlyStrokeElement
{
    public override bool IsFull => Strokes.Any() && DoExecutors.Any(e => e != null);

    public override int GestureCount => DoExecutors.Count;

    public StrokeSequence Strokes { get; }

    private readonly List<ExecuteAction> doExecutors = new();
    public IReadOnlyList<ExecuteAction> DoExecutors => doExecutors;

    public StrokeElement(params StrokeDirection[] strokes)
    {
        Strokes = new StrokeSequence(strokes);
    }

    public StrokeElement Do(ExecuteAction executor)
    {
        doExecutors.Add(executor);
        return this;
    }
}
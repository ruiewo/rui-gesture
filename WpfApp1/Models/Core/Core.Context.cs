using RuiGesture.Models.WinApi;

namespace RuiGesture.Models.Core;

using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

public class EvaluationContext
{
    public readonly Point GestureStartPosition;
    public readonly ForegroundWindowInfo ForegroundWindow;
    public readonly PointedWindowInfo PointedWindow;

    public EvaluationContext(Point gestureStartPosition)
    {
        GestureStartPosition = gestureStartPosition;
        ForegroundWindow = new ForegroundWindowInfo();
        PointedWindow = new PointedWindowInfo(gestureStartPosition);
    }
}

public class ExecutionContext
{
    public readonly Point GestureStartPosition;
    public readonly Point GestureEndPosition;
    public readonly ForegroundWindowInfo ForegroundWindow;
    public readonly PointedWindowInfo PointedWindow;

    public ExecutionContext(
        EvaluationContext evaluationContext,
        Point gestureEndPosition)
    {
        GestureStartPosition = evaluationContext.GestureStartPosition;
        GestureEndPosition = gestureEndPosition;
        ForegroundWindow = evaluationContext.ForegroundWindow;
        PointedWindow = evaluationContext.PointedWindow;
    }
}

public delegate bool EvaluateAction(EvaluationContext ctx);

public delegate void ExecuteAction(ExecutionContext ctx);

public abstract class ContextManager<EvaluationContext, ExecutionContext>
{
    public virtual EvaluationContext CreateEvaluateContext()
        => throw new NotImplementedException();

    public virtual ExecutionContext CreateExecutionContext(EvaluationContext evaluationContext)
        => throw new NotImplementedException();

    public virtual IReadOnlyList<IReadOnlyWhenElement> Evaluate(
        EvaluationContext evalContext,
        IEnumerable<IReadOnlyWhenElement> whenElements)
        => whenElements.Where(w => w.WhenEvaluator(evalContext)).ToList();

    public virtual void Execute(ExecutionContext execContext, ExecuteAction executeAction)
        => executeAction(execContext);

    private void Execute(
        EvaluationContext evalContext,
        IEnumerable<ExecuteAction> executeActions)
    {
        var execContext = CreateExecutionContext(evalContext);
        foreach (var executor in executeActions)
        {
            Execute(execContext, executor);
        }
    }

    public void ExecutePressExecutors(
        EvaluationContext evalContext,
        IEnumerable<IReadOnlyDoubleThrowElement> doubleThrowElements)
    {
        if (doubleThrowElements.Any())
        {
            foreach (var element in doubleThrowElements)
            {
                Execute(evalContext, element.PressExecutors);
            }
        }
    }

    public void ExecutePressExecutors(
        EvaluationContext evalContext,
        IEnumerable<IReadOnlyDecomposedElement> decomposedElements)
    {
        if (decomposedElements.Any())
        {
            foreach (var element in decomposedElements)
            {
                Execute(evalContext, element.PressExecutors);
            }
        }
    }

    public void ExecuteDoExecutors(
        EvaluationContext evalContext,
        IEnumerable<IReadOnlyDoubleThrowElement> doubleThrowElements)
    {
        if (doubleThrowElements.Any())
        {
            foreach (var element in doubleThrowElements)
            {
                Execute(evalContext, element.DoExecutors);
            }
        }
    }

    public void ExecuteDoExecutors(
        EvaluationContext evalContext,
        IEnumerable<IReadOnlySingleThrowElement> singleThrowElements)
    {
        if (singleThrowElements.Any())
        {
            foreach (var element in singleThrowElements)
            {
                Execute(evalContext, element.DoExecutors);
            }
        }
    }

    public void ExecuteDoExecutors(
        EvaluationContext evalContext,
        IEnumerable<IReadOnlyStrokeElement> strokeElements)
    {
        if (strokeElements.Any())
        {
            foreach (var element in strokeElements)
            {
                Execute(evalContext, element.DoExecutors);
            }
        }
    }

    public void ExecuteReleaseExecutors(
        EvaluationContext evalContext,
        IEnumerable<IReadOnlyDoubleThrowElement> doubleThrowElements)
    {
        if (doubleThrowElements.Any())
        {
            foreach (var element in doubleThrowElements)
            {
                Execute(evalContext, element.ReleaseExecutors);
            }
        }
    }

    public void ExecuteReleaseExecutors(
        EvaluationContext evalContext,
        IEnumerable<IReadOnlyDecomposedElement> decomposedElements)
    {
        if (decomposedElements.Any())
        {
            foreach (var element in decomposedElements)
            {
                Execute(evalContext, element.ReleaseExecutors);
            }
        }
    }
}
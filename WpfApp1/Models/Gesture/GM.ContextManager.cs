namespace RuiGesture.Models.Gesture;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using Core;

public class ContextManager : ContextManager<EvaluationContext, Core.ExecutionContext>, IDisposable
{
    public Point CursorPosition { get; set; }

    public int EvaluationLimitTime { get; } = 1000; // ms

    public override EvaluationContext CreateEvaluateContext() => new(CursorPosition);

    public override Core.ExecutionContext CreateExecutionContext(EvaluationContext evaluationContext) =>
        new(evaluationContext, CursorPosition);

    private readonly LowLatencyScheduler _evaluationScheduler =
        new(
            "EvaluationTaskScheduler",
            ThreadPriority.AboveNormal,
            Math.Max(2, Environment.ProcessorCount / 2));

    private readonly LowLatencyScheduler _executionScheduler =
        new(
            "ExecutionTaskScheduler",
            ThreadPriority.AboveNormal,
            Math.Max(2, Environment.ProcessorCount / 2));

    private TaskFactory _evaluationTaskFactory => new(_evaluationScheduler);

    private TaskFactory _executionTaskFactory => new(_executionScheduler);

    private async Task<bool> EvaluateAsync(
        EvaluationContext evalContext,
        IReadOnlyWhenElement whenElement)
    {
        var task = _evaluationTaskFactory.StartNew(() => { return whenElement.WhenEvaluator(evalContext); });
        try
        {
            if (await Task.WhenAny(task, Task.Delay(EvaluationLimitTime)).ConfigureAwait(false) == task)
            {
                return task.Result;
            }
            else
            {
                Verbose.Error(
                    $"Evaluation of WhenEvaluator was timeout; (EvaluationLimitTime: {EvaluationLimitTime} ms)");
            }
        }
        catch (AggregateException ex)
        {
            Verbose.Error($"An exception was thrown while evaluating an evaluator: {ex.InnerException.ToString()}");
        }
        catch (Exception ex)
        {
            Verbose.Error($"An unexpected exception was thrown while evaluating an evaluator: {ex.ToString()}");
        }

        return false;
    }

    public override IReadOnlyList<IReadOnlyWhenElement> Evaluate(
        EvaluationContext evalContext,
        IEnumerable<IReadOnlyWhenElement> whenElements)
        => whenElements.AsParallel()
            .WithDegreeOfParallelism(Math.Max(2, Environment.ProcessorCount / 2))
            .Where(w => w.WhenEvaluator(evalContext)).ToList();

    public override void Execute(
        Core.ExecutionContext execContext,
        ExecuteAction executeAction)
    {
        var task = _executionTaskFactory.StartNew(() => { executeAction(execContext); });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _evaluationScheduler.Dispose();
            _executionScheduler.Dispose();
        }
    }

    ~ContextManager() => Dispose(false);
}
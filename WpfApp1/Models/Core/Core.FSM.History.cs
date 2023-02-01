namespace RuiGesture.Models.Core;

using System.Collections.Generic;
using System.Linq;

public class HistoryRecord<TConfig, TContextManager>
    where TConfig : GestureMachineConfig
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly PhysicalReleaseEvent ReleaseEvent;
    public readonly State<TConfig, TContextManager> State;

    public HistoryRecord(
        PhysicalReleaseEvent releaseEvent,
        State<TConfig, TContextManager> state)
    {
        ReleaseEvent = releaseEvent;
        State = state;
    }
}

public static class HistoryRecord
{
    public static HistoryRecord<TConfig, TContextManager>
        Create<TConfig, TContextManager>(
            PhysicalReleaseEvent releaseEvent,
            State<TConfig, TContextManager> state)
        where TConfig : GestureMachineConfig
        where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
    {
        return new HistoryRecord<TConfig, TContextManager>(releaseEvent, state);
    }
}

public class HistoryQueryResult<TConfig, TContextManager>
    where TConfig : GestureMachineConfig
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly State<TConfig, TContextManager> FoundState;
    public readonly IReadOnlyList<PhysicalReleaseEvent> SkippedReleaseEvents;

    public HistoryQueryResult(
        State<TConfig, TContextManager> foundState,
        IReadOnlyList<PhysicalReleaseEvent> skippedReleaseEvents)
    {
        FoundState = foundState;
        SkippedReleaseEvents = skippedReleaseEvents;
    }
}

public class History<TConfig, TContextManager>
    where TConfig : GestureMachineConfig
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly IReadOnlyList<HistoryRecord<TConfig, TContextManager>> Records;

    public History(
        PhysicalReleaseEvent releaseEvent,
        State<TConfig, TContextManager> state)
        : this(new List<HistoryRecord<TConfig, TContextManager>>()
        {
            HistoryRecord.Create(releaseEvent, state),
        })
    {
    }

    public History(IReadOnlyList<HistoryRecord<TConfig, TContextManager>> records)
    {
        Records = records;
    }

    public HistoryQueryResult<TConfig, TContextManager>
        Query(PhysicalReleaseEvent releaseEvent)
    {
        var nextHistory = Records.TakeWhile(t => t.ReleaseEvent != releaseEvent);
        var foundState = Records[nextHistory.Count()].State;
        var skippedReleaseEvents = Records.Skip(nextHistory.Count()).Select(t => t.ReleaseEvent).ToList();
        return new HistoryQueryResult<TConfig, TContextManager>(foundState,
            skippedReleaseEvents);
    }

    public History<TConfig, TContextManager>
        CreateNext(PhysicalReleaseEvent releaseEvent, State<TConfig, TContextManager> state)
    {
        var newRecords = Records.ToList();
        newRecords.Add(HistoryRecord.Create(releaseEvent, state));
        return new History<TConfig, TContextManager>(newRecords);
    }
}

public static class History
{
    public static History<TConfig, TContextManager>
        Create<TConfig, TContextManager>(
            PhysicalReleaseEvent releaseEvent,
            State<TConfig, TContextManager> state)
        where TConfig : GestureMachineConfig
        where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
    {
        return new History<TConfig, TContextManager>(releaseEvent, state);
    }

    public static History<TConfig, TContextManager>
        Create<TConfig, TContextManager>(
            IReadOnlyList<HistoryRecord<TConfig, TContextManager>> records)
        where TConfig : GestureMachineConfig
        where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
    {
        return new History<TConfig, TContextManager>(records);
    }
}
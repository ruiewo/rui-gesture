namespace RuiGesture.Models.Core;

using System.Collections.Generic;
using System.Linq;

public class HistoryRecord<TContextManager>
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly PhysicalReleaseEvent ReleaseEvent;
    public readonly State<TContextManager> State;

    public HistoryRecord(
        PhysicalReleaseEvent releaseEvent,
        State<TContextManager> state)
    {
        ReleaseEvent = releaseEvent;
        State = state;
    }
}

public static class HistoryRecord
{
    public static HistoryRecord<TContextManager>
        Create<TContextManager>(
            PhysicalReleaseEvent releaseEvent,
            State<TContextManager> state)
        where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
    {
        return new HistoryRecord<TContextManager>(releaseEvent, state);
    }
}

public class HistoryQueryResult<TContextManager>
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly State<TContextManager> FoundState;
    public readonly IReadOnlyList<PhysicalReleaseEvent> SkippedReleaseEvents;

    public HistoryQueryResult(
        State<TContextManager> foundState,
        IReadOnlyList<PhysicalReleaseEvent> skippedReleaseEvents)
    {
        FoundState = foundState;
        SkippedReleaseEvents = skippedReleaseEvents;
    }
}

public class History<TContextManager>
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly IReadOnlyList<HistoryRecord<TContextManager>> Records;

    public History(
        PhysicalReleaseEvent releaseEvent,
        State<TContextManager> state)
        : this(new List<HistoryRecord<TContextManager>>()
        {
            HistoryRecord.Create(releaseEvent, state),
        })
    {
    }

    public History(IReadOnlyList<HistoryRecord<TContextManager>> records)
    {
        Records = records;
    }

    public HistoryQueryResult<TContextManager>
        Query(PhysicalReleaseEvent releaseEvent)
    {
        var nextHistory = Records.TakeWhile(t => t.ReleaseEvent != releaseEvent);
        var foundState = Records[nextHistory.Count()].State;
        var skippedReleaseEvents = Records.Skip(nextHistory.Count()).Select(t => t.ReleaseEvent).ToList();
        return new HistoryQueryResult<TContextManager>(foundState,
            skippedReleaseEvents);
    }

    public History<TContextManager>
        CreateNext(PhysicalReleaseEvent releaseEvent, State<TContextManager> state)
    {
        var newRecords = Records.ToList();
        newRecords.Add(HistoryRecord.Create(releaseEvent, state));
        return new History<TContextManager>(newRecords);
    }
}

public static class History
{
    public static History<TContextManager>
        Create<TContextManager>(PhysicalReleaseEvent releaseEvent, State<TContextManager> state)
        where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
    {
        return new History<TContextManager>(releaseEvent, state);
    }

    public static History<TContextManager>
        Create<TContextManager>(
            IReadOnlyList<HistoryRecord<TContextManager>> records)
        where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
    {
        return new History<TContextManager>(records);
    }
}
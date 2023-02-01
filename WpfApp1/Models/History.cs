namespace RuiGesture.Models;

using System.Collections.Generic;
using System.Linq;
using Event;
using Gesture;

public class HistoryRecord
{
    public readonly PhysicalReleaseEvent ReleaseEvent;
    public readonly State State;

    public HistoryRecord(
        PhysicalReleaseEvent releaseEvent,
        State state)
    {
        ReleaseEvent = releaseEvent;
        State = state;
    }

    public static HistoryRecord Create(PhysicalReleaseEvent releaseEvent, State state)
    {
        return new HistoryRecord(releaseEvent, state);
    }
}

public class HistoryQueryResult
{
    public readonly State FoundState;
    public readonly IReadOnlyList<PhysicalReleaseEvent> SkippedReleaseEvents;

    public HistoryQueryResult(
        State foundState,
        IReadOnlyList<PhysicalReleaseEvent> skippedReleaseEvents)
    {
        FoundState = foundState;
        SkippedReleaseEvents = skippedReleaseEvents;
    }
}

public class History
{
    public readonly IReadOnlyList<HistoryRecord> Records;

    public History(PhysicalReleaseEvent releaseEvent, State state)
        : this(new List<HistoryRecord> { HistoryRecord.Create(releaseEvent, state), })
    {
    }

    public History(IReadOnlyList<HistoryRecord> records)
    {
        Records = records;
    }

    public HistoryQueryResult Query(PhysicalReleaseEvent releaseEvent)
    {
        var nextHistory = Records.TakeWhile(t => t.ReleaseEvent != releaseEvent);
        var foundState = Records[nextHistory.Count()].State;
        var skippedReleaseEvents = Records.Skip(nextHistory.Count()).Select(t => t.ReleaseEvent).ToList();
        return new HistoryQueryResult(foundState, skippedReleaseEvents);
    }

    public History CreateNext(PhysicalReleaseEvent releaseEvent, State state)
    {
        var newRecords = Records.ToList();
        newRecords.Add(HistoryRecord.Create(releaseEvent, state));
        return new History(newRecords);
    }

    public static History Create(PhysicalReleaseEvent releaseEvent, State state)
    {
        return new History(releaseEvent, state);
    }

    public static History Create(IReadOnlyList<HistoryRecord> records)
    {
        return new History(records);
    }
}
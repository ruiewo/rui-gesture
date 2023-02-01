using RuiGesture.Models.DSL;
using RuiGesture.Models.Event;

namespace RuiGesture.Models.Gesture;

using System.Collections.Generic;
using System.Linq;
using System.Threading;

public abstract class State
{
    public State(int depth)
    {
        Depth = depth;
    }

    public int Depth { get; }

    public virtual Result Input(IPhysicalEvent evnt)
        => StateResult.Create(false, this);

    public virtual State Timeout() => this;

    public virtual State Reset() => this;

    protected static bool HasPressExecutors(
        IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => doubleThrowElements.Any(d => d.PressExecutors.Any());

    protected static bool HasDoExecutors(
        IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => doubleThrowElements.Any(d => d.DoExecutors.Any());

    protected static bool HasReleaseExecutors(
        IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => doubleThrowElements.Any(d => d.ReleaseExecutors.Any());

    public bool IsState0 => GetType() == typeof(State0);
    public bool IsStateN => GetType() == typeof(StateN);

    public State0 AsState0() => this as State0;

    public StateN AsStateN() => this as StateN;
}

public struct Result
{
    public readonly bool EventIsConsumed;
    public readonly State NextState;

    public Result(bool eventIsConsumed, State nextState)
    {
        EventIsConsumed = eventIsConsumed;
        NextState = nextState;
    }
}

public static class StateResult
{
    public static Result Create(bool eventIsConsumed, State nextState)
    {
        return new Result(eventIsConsumed, nextState);
    }
}
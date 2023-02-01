namespace RuiGesture.Models.Core;

using System.Collections.Generic;
using System.Linq;

public abstract class State<TContextManager>
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public int Depth { get; }

    public State(int depth)
    {
        Depth = depth;
    }

    public virtual Result<TContextManager> Input(IPhysicalEvent evnt)
        => Result.Create(false, this);

    public virtual State<TContextManager> Timeout()
        => this;

    public virtual State<TContextManager> Reset()
        => this;

    protected static bool HasPressExecutors(
        IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => doubleThrowElements.Any(d => d.PressExecutors.Any());

    protected static bool HasDoExecutors(
        IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => doubleThrowElements.Any(d => d.DoExecutors.Any());

    protected static bool HasReleaseExecutors(
        IReadOnlyList<IReadOnlyDoubleThrowElement> doubleThrowElements)
        => doubleThrowElements.Any(d => d.ReleaseExecutors.Any());

    public bool IsState0 => GetType() == typeof(State0<TContextManager>);
    public bool IsStateN => GetType() == typeof(StateN<TContextManager>);

    public State0<TContextManager> AsState0()
        => this as State0<TContextManager>;

    public StateN<TContextManager> AsStateN()
        => this as StateN<TContextManager>;
}
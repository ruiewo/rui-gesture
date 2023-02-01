namespace RuiGesture.Models.Core;

using System.Collections.Generic;
using System.Linq;

public abstract class State<TConfig, TContextManager>
    where TConfig : GestureMachineConfig
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public int Depth { get; }

    public State(int depth)
    {
        Depth = depth;
    }

    public virtual Result<TConfig, TContextManager> Input(IPhysicalEvent evnt)
        => Result.Create(false, this);

    public virtual State<TConfig, TContextManager> Timeout()
        => this;

    public virtual State<TConfig, TContextManager> Reset()
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

    public bool IsState0 => GetType() == typeof(State0<TConfig, TContextManager>);
    public bool IsStateN => GetType() == typeof(StateN<TConfig, TContextManager>);

    public State0<TConfig, TContextManager> AsState0()
        => this as State0<TConfig, TContextManager>;

    public StateN<TConfig, TContextManager> AsStateN()
        => this as StateN<TConfig, TContextManager>;
}
namespace RuiGesture.Models.Core;

public struct Result<TConfig, TContextManager>
    where TConfig : GestureMachineConfig
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly bool EventIsConsumed;
    public readonly State<TConfig, TContextManager> NextState;

    public Result(bool eventIsConsumed, State<TConfig, TContextManager> nextState)
    {
        EventIsConsumed = eventIsConsumed;
        NextState = nextState;
    }
}

public static class Result
{
    public static Result<TConfig, TContextManager>
        Create<TConfig, TContextManager>(
            bool eventIsConsumed,
            State<TConfig, TContextManager> nextState)
        where TConfig : GestureMachineConfig
        where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
    {
        return new Result<TConfig, TContextManager>(eventIsConsumed, nextState);
    }
}
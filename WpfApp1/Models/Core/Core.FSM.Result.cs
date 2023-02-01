namespace RuiGesture.Models.Core;

public struct Result<TContextManager>
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly bool EventIsConsumed;
    public readonly State<TContextManager> NextState;

    public Result(bool eventIsConsumed, State<TContextManager> nextState)
    {
        EventIsConsumed = eventIsConsumed;
        NextState = nextState;
    }
}

public static class Result
{
    public static Result<TContextManager>
        Create<TContextManager>(bool eventIsConsumed, State<TContextManager> nextState)
        where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
    {
        return new Result<TContextManager>(eventIsConsumed, nextState);
    }
}
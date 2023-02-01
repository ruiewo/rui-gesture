namespace RuiGesture.Models.Gesture;

using System.Drawing;
using WinApi;

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
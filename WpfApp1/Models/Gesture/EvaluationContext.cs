namespace RuiGesture.Models.Gesture;

using System.Drawing;
using WinApi;

public class EvaluationContext
{
    public readonly Point GestureStartPosition;
    public readonly ForegroundWindowInfo ForegroundWindow;
    public readonly PointedWindowInfo PointedWindow;

    public EvaluationContext(Point gestureStartPosition)
    {
        GestureStartPosition = gestureStartPosition;
        ForegroundWindow = new ForegroundWindowInfo();
        PointedWindow = new PointedWindowInfo(gestureStartPosition);
    }
}
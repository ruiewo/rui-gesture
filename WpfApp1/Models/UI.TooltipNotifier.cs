namespace RuiGesture.Models;

using System;
using System.Drawing;
using System.Reflection;

public class TooltipNotifier : IDisposable
{
    private const int Absolute = 0x0002;

    private IWin32Window win;
    private MethodInfo SetTool;
    private MethodInfo SetTrackPosition;
    private MethodInfo StartTimer;

    private ToolTip tooltip = new ToolTip();

    public TooltipNotifier(IWin32Window win)
    {
        this.win = win;
        SetTool = tooltip.GetType().GetMethod("SetTool", BindingFlags.Instance | BindingFlags.NonPublic);
        SetTrackPosition =
            tooltip.GetType().GetMethod("SetTrackPosition", BindingFlags.Instance | BindingFlags.NonPublic);
        StartTimer = tooltip.GetType().GetMethod("StartTimer", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    public void Show(string text, Point point, int duration)
    {
        SetTrackPosition.Invoke(tooltip, new object[] { point.X, point.Y, });
        SetTool.Invoke(tooltip, new object[] { win, text, Absolute, point, });
        StartTimer.Invoke(tooltip, new object[] { win, duration, });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            tooltip.Dispose();
        }
    }

    ~TooltipNotifier() => Dispose(false);
}
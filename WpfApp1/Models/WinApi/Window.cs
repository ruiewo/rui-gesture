namespace RuiGesture.Models.WinApi;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

public static class Window
{
    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        public static extern bool GetPhysicalCursorPos(out Point lpPoint);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    }

    // http://pinvoke.net/default.aspx/Enums/ChildWindowFromPointFlags.html
    /// <summary>
    /// For use with ChildWindowFromPointEx 
    /// </summary>
    [Flags]
    public enum WindowFromPointFlags : int
    {
        /// <summary>
        /// Does not skip any child windows
        /// </summary>
        CWP_ALL = 0x0000,

        /// <summary>
        /// Skips invisible child windows
        /// </summary>
        CWP_SKIPINVISIBLE = 0x0001,

        /// <summary>
        /// Skips disabled child windows
        /// </summary>
        CWP_SKIPDISABLED = 0x0002,

        /// <summary>
        /// Skips transparent child windows
        /// </summary>
        CWP_SKIPTRANSPARENT = 0x0004,
    }

    public static WindowInfo From(IntPtr hWnd)
    {
        return new WindowInfo(hWnd);
    }

    /// <summary>
    /// A shortcut to GetCursorPos().
    /// </summary>
    /// <returns>Physical cursor position.</returns>
    public static Point GetCursorPos()
    {
        var point = new Point();
        NativeMethods.GetCursorPos(out point);
        return point;
    }

    /// <summary>
    /// Returns logical cursor position culculated based on physical and logical screen size.
    /// </summary>
    /// <returns>Logical cursor position.</returns>
    public static Point GetLogicalCursorPos()
    {
        var point = GetPhysicalCursorPos();
        var scaleFactor = (float)Device.GetLogicalScreenSize().X / (float)Device.GetPhysicalScreenSize().X;
        var x = (int)(point.X * scaleFactor);
        var y = (int)(point.Y * scaleFactor);
        return new Point(x, y);
    }

    /// <summary>
    /// A shortcut to GetPhysicalCursorPos().
    /// </summary>
    /// <returns>Physical cursor position.</returns>
    public static Point GetPhysicalCursorPos()
    {
        var point = new Point();
        NativeMethods.GetPhysicalCursorPos(out point);
        return point;
    }

    public static WindowInfo GetForegroundWindow()
    {
        return new ForegroundWindowInfo();
    }

    public static WindowInfo GetPointedWindow()
    {
        return WindowFromPoint(GetPhysicalCursorPos());
    }

    public static WindowInfo WindowFromPoint(Point point)
    {
        return new PointedWindowInfo(point);
    }

    public static WindowInfo FindWindow(string lpClassName, string lpWindowName)
    {
        return From(NativeMethods.FindWindow(lpClassName, lpWindowName));
    }

    public static IReadOnlyList<WindowInfo> GetTopLevelWindows()
    {
        return new TopLevelWindows().ToList();
    }

    public static IReadOnlyList<WindowInfo> GetThreadWindows(int threadId)
    {
        return new ThreadWindows(threadId).ToList();
    }
}
namespace RuiGesture.Models.WinApi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections;
using System.Drawing;

// http://qiita.com/katabamisan/items/081547f42512e93a31ab

public abstract class WindowEnumerable : IEnumerable<WindowInfo>
{
    internal delegate bool EnumWindowsProcDelegate(IntPtr hWnd, IntPtr lParam);

    internal readonly List<IntPtr> handles = new();

    public IEnumerator<WindowInfo> GetEnumerator()
        => handles.Select(x => new WindowInfo(x)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    internal bool EnumWindowProc(IntPtr handle, IntPtr lParam)
    {
        handles.Add(handle);
        return true;
    }
}

public sealed class TopLevelWindows : WindowEnumerable
{
    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsProcDelegate lpEnumFunc, IntPtr lParam);
    }

    public TopLevelWindows()
    {
        NativeMethods.EnumWindows(EnumWindowProc, IntPtr.Zero);
    }
}

public sealed class ChildWindows : WindowEnumerable
{
    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumChildWindows(IntPtr hwndParent,
            EnumWindowsProcDelegate lpEnumFunc,
            IntPtr lParam);
    }

    public readonly IntPtr WindowHandle;

    public ChildWindows(IntPtr hWnd)
    {
        WindowHandle = hWnd;
        NativeMethods.EnumChildWindows(hWnd, EnumWindowProc, IntPtr.Zero);
    }
}

public sealed class PointedDescendantWindows : WindowEnumerable
{
    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPointEx(IntPtr hWndParent,
            Point pt,
            Window.WindowFromPointFlags uFlags);
    }

    public readonly IntPtr WindowHandle;
    public readonly Point Point;

    public PointedDescendantWindows(IntPtr hWnd, Point point, Window.WindowFromPointFlags flags)
    {
        WindowHandle = hWnd;
        Point = point;
        if (hWnd != IntPtr.Zero)
        {
            var res = ChildWindowFromPointEx(hWnd, point, flags);
            while (hWnd != res && res != IntPtr.Zero)
            {
                hWnd = res;
                handles.Add(hWnd);
                res = ChildWindowFromPointEx(hWnd, point, flags);
            }
        }
    }

    private IntPtr ChildWindowFromPointEx(IntPtr hWnd, Point point, Window.WindowFromPointFlags flags)
    {
        var clientPoint = new Point(point.X, point.Y);
        NativeMethods.ScreenToClient(hWnd, ref clientPoint);
        return NativeMethods.ChildWindowFromPointEx(hWnd, clientPoint, flags);
    }
}

public sealed class ThreadWindows : WindowEnumerable
{
    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumThreadWindows(int dwThreadId, EnumWindowsProcDelegate lpfn, IntPtr lParam);
    }

    public readonly int ThreadId;

    public ThreadWindows(int threadId)
    {
        ThreadId = threadId;
        NativeMethods.EnumThreadWindows(threadId, EnumWindowProc, IntPtr.Zero);
    }
}
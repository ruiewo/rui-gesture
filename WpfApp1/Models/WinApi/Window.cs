﻿namespace RuiGesture.Models.WinApi;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public class WindowInfo
{
    protected static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, WindowLongParam nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess,
            int dwFlags,
            StringBuilder lpExeName,
            out int lpdwSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern long SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent,
            IntPtr hwndChildAfter,
            string lpszClass,
            string lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetAncestor(IntPtr hwnd, int flags);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPhysicalPoint(Point point);
    }

    public const int MaxPathSize = 1024;

    // http://www.pinvoke.net/default.aspx/kernel32/OpenProcess.html
    [Flags]
    public enum ProcessAccessFlags : int
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x000000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000,
    }

    // http://pinvoke.net/default.aspx/Constants/GWL%20-%20GetWindowLong.html
    public enum WindowLongParam : int
    {
        /// <summary>Sets a new address for the window procedure.</summary>
        /// <remarks>You cannot change this attribute if the window does not belong to the same process as the calling thread.</remarks>
        GWL_WNDPROC = -4,

        /// <summary>Sets a new application instance handle.</summary>
        GWLP_HINSTANCE = -6,

        GWLP_HWNDPARENT = -8,

        /// <summary>Sets a new identifier of the child window.</summary>
        /// <remarks>The window cannot be a top-level window.</remarks>
        GWL_ID = -12,

        /// <summary>Sets a new window style.</summary>
        GWL_STYLE = -16,

        /// <summary>Sets a new extended window style.</summary>
        /// <remarks>See <see cref="ExWindowStyles"/>.</remarks>
        GWL_EXSTYLE = -20,

        /// <summary>Sets the user data associated with the window.</summary>
        /// <remarks>This data is intended for use by the application that created the window. Its value is initially zero.</remarks>
        GWL_USERDATA = -21,

        /// <summary>Sets the return value of a message processed in the dialog box procedure.</summary>
        /// <remarks>Only applies to dialog boxes.</remarks>
        DWLP_MSGRESULT = 0,

        /// <summary>Sets new extra information that is private to the application, such as handles or pointers.</summary>
        /// <remarks>Only applies to dialog boxes.</remarks>
        DWLP_USER = 8,

        /// <summary>Sets the new address of the dialog box procedure.</summary>
        /// <remarks>Only applies to dialog boxes.</remarks>
        DWLP_DLGPROC = 4,
    }

    public readonly IntPtr WindowHandle;

    private readonly Lazy<Tuple<int, int>> threadProcessId;

    public int ThreadId
    {
        get { return threadProcessId.Value.Item1; }
    }

    public int ProcessId
    {
        get { return threadProcessId.Value.Item2; }
    }

    private readonly Lazy<IntPtr> windowId;

    public IntPtr WindowId
    {
        get { return windowId.Value; }
    }

    public string Text
    {
        get { return GetWindowText(WindowHandle); }
    }

    private readonly Lazy<string> className;

    public string ClassName
    {
        get { return className.Value; }
    }

    private readonly Lazy<WindowInfo> parent;

    public WindowInfo Parent
    {
        get { return parent.Value; }
    }

    private Lazy<string> modulePath;

    public string ModulePath
    {
        get { return modulePath.Value; }
    }

    private Lazy<string> moduleName;

    public string ModuleName
    {
        get { return moduleName.Value; }
    }


    public WindowInfo(IntPtr hWnd)
    {
        WindowHandle = hWnd;
        threadProcessId = new Lazy<Tuple<int, int>>(() => { return GetThreadProcessId(WindowHandle); });
        windowId = new Lazy<IntPtr>(() => { return GetWindowId(WindowHandle); });
        className = new Lazy<string>(() => { return GetClassName(WindowHandle); });
        parent = new Lazy<WindowInfo>(() =>
        {
            var res = NativeMethods.GetParent(WindowHandle);
            if (res == null)
            {
                return null;
            }
            else
            {
                return new WindowInfo(res);
            }
        });
        modulePath = new Lazy<string>(() => { return GetPath(ProcessId); });
        moduleName = new Lazy<string>(() => { return GetName(ModulePath); });
    }

    private static Tuple<int, int> GetThreadProcessId(IntPtr hWnd)
    {
        var tid = NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
        return Tuple.Create(tid, pid);
    }

    private static IntPtr GetWindowId(IntPtr hWnd)
    {
        return NativeMethods.GetWindowLong(hWnd, WindowLongParam.GWL_ID);
    }

    private static string GetWindowText(IntPtr hWnd)
    {
        var buffer = new StringBuilder(MaxPathSize);
        NativeMethods.GetWindowText(hWnd, buffer, MaxPathSize);
        return buffer.ToString();
    }

    private static string GetClassName(IntPtr hWnd)
    {
        var buffer = new StringBuilder(MaxPathSize);
        NativeMethods.GetClassName(hWnd, buffer, MaxPathSize);
        return buffer.ToString();
    }

    private static string GetPath(int pid)
    {
        var buffer = new StringBuilder(MaxPathSize);
        var hProcess =
            NativeMethods.OpenProcess(
                ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VirtualMemoryRead, false, pid);
        var lpdwSize = MaxPathSize;
        try
        {
            NativeMethods.QueryFullProcessImageName(hProcess, 0, buffer, out lpdwSize);
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }

        return buffer.ToString();
    }

    private static string GetName(string path)
    {
        return path.Substring(path.LastIndexOf("\\") + 1);
    }

    public bool SetForegroundWindow()
    {
        return NativeMethods.SetForegroundWindow(WindowHandle);
    }

    public bool Activate()
    {
        var hwndTarget = NativeMethods.GetAncestor(WindowHandle, 2);
        var hwndActive = NativeMethods.GetForegroundWindow();
        if (hwndTarget == hwndActive)
        {
            return true;
        }

        var tidTarget = NativeMethods.GetWindowThreadProcessId(WindowHandle, out var outTmp);
        var tidActive = NativeMethods.GetWindowThreadProcessId(hwndActive, out outTmp);

        if (NativeMethods.SetForegroundWindow(WindowHandle))
        {
            return true;
        }

        if (tidTarget == tidActive)
        {
            return BringWindowToTop();
        }

        NativeMethods.AttachThreadInput(tidTarget, tidActive, true);
        try
        {
            return BringWindowToTop();
        }
        finally
        {
            NativeMethods.AttachThreadInput(tidTarget, tidActive, false);
        }
    }

    public bool BringWindowToTop()
    {
        return NativeMethods.BringWindowToTop(WindowHandle);
    }

    public long SendMessage(int Msg, int wParam, int lParam)
    {
        return NativeMethods.SendMessage(WindowHandle, Msg, wParam, lParam);
    }

    public bool PostMessage(int Msg, int wParam, int lParam)
    {
        return NativeMethods.PostMessage(WindowHandle, Msg, wParam, lParam);
    }

    public WindowInfo FindWindowEx(IntPtr hwndChildAfter, string lpszClass, string lpszWindow)
    {
        return new WindowInfo(NativeMethods.FindWindowEx(WindowHandle, hwndChildAfter, lpszClass, lpszWindow));
    }

    public WindowInfo FindWindowEx(string lpszClass, string lpszWindow)
    {
        return new WindowInfo(NativeMethods.FindWindowEx(WindowHandle, IntPtr.Zero, lpszClass, lpszWindow));
    }

    public IReadOnlyList<WindowInfo> GetChildWindows()
    {
        return new ChildWindows(WindowHandle).ToList();
    }

    public IReadOnlyList<WindowInfo> GetPointedDescendantWindows(Point point, Window.WindowFromPointFlags flags)
    {
        return new PointedDescendantWindows(WindowHandle, point, flags).ToList();
    }

    public IReadOnlyList<WindowInfo> GetPointedDescendantWindows(Point point)
    {
        return new PointedDescendantWindows(WindowHandle, point,
            Window.WindowFromPointFlags.CWP_ALL).ToList();
    }
}

public class ForegroundWindowInfo : WindowInfo
{
    public ForegroundWindowInfo() : base(NativeMethods.GetForegroundWindow())
    {
    }
}

public class PointedWindowInfo : WindowInfo
{
    private readonly Point point;

    public IReadOnlyList<WindowInfo> GetPointedDescendantWindows()
    {
        return new PointedDescendantWindows(WindowHandle, point,
            Window.WindowFromPointFlags.CWP_ALL).ToList();
    }

    public PointedWindowInfo(Point point) : base(NativeMethods.WindowFromPhysicalPoint(point))
    {
        this.point = point;
    }
}

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
        public static extern bool EnumThreadWindows(int dwThreadId,
            EnumWindowsProcDelegate lpfn,
            IntPtr lParam);
    }

    public readonly int ThreadId;

    public ThreadWindows(int threadId)
    {
        ThreadId = threadId;
        NativeMethods.EnumThreadWindows(threadId, EnumWindowProc, IntPtr.Zero);
    }
}

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
namespace RuiGesture.Models.Hook;

using Enums;
using Models;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class WindowsHook : IDisposable
{
    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            SystemCallback callback,
            IntPtr hInstance,
            int threadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string name);
    }

    private delegate IntPtr SystemCallback(int nCode, IntPtr wParam, IntPtr lParam);

    protected delegate Result UserCallback(IntPtr wParam, IntPtr lParam);

    public const int HC_ACTION = 0;


    public enum Result
    {
        Transfer,
        Determine,
        Cancel,
    };

    private static readonly IntPtr LRESULTCancel = new(1);

    // These callback functions should be hold as a local variable to prevent it from GC.
    private readonly UserCallback _userCallback;
    private readonly SystemCallback _systemCallback;

    private readonly HookType _hookType;

    private IntPtr _hHook = IntPtr.Zero;

    protected WindowsHook(HookType hookType, UserCallback userCallback)
    {
        _hookType = hookType;
        _userCallback = userCallback;
        _systemCallback = Callback;
    }

    public bool IsActivated
        => _hHook != IntPtr.Zero;

    public void SetHook()
    {
        if (IsActivated)
        {
            throw new InvalidOperationException();
        }

        var log = new WinAPILogger("SetWindowsHookEx");
        log.Add($"hookType: {Enum.GetName(typeof(HookType), _hookType)}");
        var hInstance = NativeMethods.GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);

        log.Add($"moduleHandle: 0x{hInstance.ToInt64():X}");
        _hHook = NativeMethods.SetWindowsHookEx((int)_hookType, _systemCallback, hInstance, 0);
        if (IsActivated)
        {
            log.Add($"hookHandle: 0x{_hHook.ToInt64():X}");
            log.Success();
        }
        else
        {
            log.FailWithErrorCode();
        }
    }

    public void Unhook()
    {
        if (!IsActivated)
        {
            throw new InvalidOperationException();
        }

        var log = new WinAPILogger("UnhookWindowsHookEx");
        log.Add($"hookType: {Enum.GetName(typeof(HookType), _hookType)}");
        log.Add($"hookHandle: 0x{_hHook.ToInt64():X}");
        if (NativeMethods.UnhookWindowsHookEx(_hHook))
        {
            log.Success();
        }
        else
        {
            log.FailWithErrorCode();
        }

        _hHook = IntPtr.Zero;
    }

    public IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            switch (_userCallback(wParam, lParam))
            {
                case Result.Transfer:
                    return NativeMethods.CallNextHookEx(_hHook, nCode, wParam, lParam);
                case Result.Cancel:
                    return LRESULTCancel;
                case Result.Determine:
                    return IntPtr.Zero;
            }
        }

        return NativeMethods.CallNextHookEx(_hHook, nCode, wParam, lParam);
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
            if (IsActivated)
            {
                Unhook();
            }
        }
    }

    ~WindowsHook()
    {
        Dispose(false);
    }
}
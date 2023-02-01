namespace RuiGesture.ViewModels;

using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using Models.Core;
using Models.WinApi;

public class MainWindowViewModel
{
    private delegate IntPtr SystemCallback(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, SystemCallback callback, IntPtr hInstance, int threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetModuleHandle(string name);

    private const int HC_ACTION = 0;

    private const int WH_MOUSE_LL = 14;

    private static readonly IntPtr LRESULTCancel = new(1);

    private IntPtr hHook = IntPtr.Zero;

    private readonly NullEvent NullEvent = new();

    public MainWindowViewModel()
    {
        SetHook();
    }


    ~MainWindowViewModel()
    {
        RemoveHook();
    }

    public IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var a = (LowLevelMouseHook.Event)wParam;
            var b = (LowLevelMouseHook.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam,
                typeof(LowLevelMouseHook.MSLLHOOKSTRUCT));

            // 何か処理をする
            if (a == LowLevelMouseHook.Event.WM_LBUTTONDOWN)
            {
                Debug.WriteLine("clicked!!!!!!!!!!!!");
                return CallNextHookEx(hHook, nCode, wParam, lParam);
            }

            if (false)
            {
                Debug.WriteLine("canceled!!!!!!!!!!!!");
                return LRESULTCancel;
            }
        }

        return CallNextHookEx(hHook, nCode, wParam, lParam);
    }

    private void SetHook()
    {
        var hInstance = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
        hHook = SetWindowsHookEx(WH_MOUSE_LL, Callback, hInstance, 0);
    }

    private void RemoveHook()
    {
        UnhookWindowsHookEx(hHook);
    }


    protected WindowsHook.Result ToHookResult(bool consumed)
        => consumed ? WindowsHook.Result.Cancel : WindowsHook.Result.Transfer;
}
namespace RuiGesture.ViewModels;

using System.Drawing;
using System.Runtime.InteropServices;
using Models;
using Models.Event;
using Models.Gesture;
using Models.Hook;
using System;
using System.Diagnostics;

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

    public virtual GestureMachine GestureMachine { get; }


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

    public WindowsHook.Result MouseProc(LowLevelMouseHook.Event evnt, LowLevelMouseHook.MSLLHOOKSTRUCT data)
    {
        if (data.FromCreviceApp)
        {
            Verbose.Print($"MouseEvent(event={Enum.GetName(typeof(LowLevelMouseHook.Event), evnt)}, " +
                          $"dwExtraInfo={BitConverter.ToString(BitConverter.GetBytes((int)data.dwExtraInfo))}) " +
                          "was passed to the next hook because this event has the signature of CreviceApp");
            return WindowsHook.Result.Transfer;
        }
        else if (data.FromTablet)
        {
            Verbose.Print($"MouseEvent(event={Enum.GetName(typeof(LowLevelMouseHook.Event), evnt)}, " +
                          $"dwExtraInfo={BitConverter.ToString(BitConverter.GetBytes((int)data.dwExtraInfo))}) " +
                          "was passed to the next hook because this event has the signature of Tablet");
            return WindowsHook.Result.Transfer;
        }

        var point = new Point(data.pt.x, data.pt.y);

        switch (evnt)
        {
            case LowLevelMouseHook.Event.WM_MOUSEMOVE:
                return ToHookResult(GestureMachine.Input(NullEvent, point));
            case LowLevelMouseHook.Event.WM_LBUTTONDOWN:
                return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.LButton.PressEvent, point));
            case LowLevelMouseHook.Event.WM_LBUTTONUP:
                return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.LButton.ReleaseEvent, point));
            case LowLevelMouseHook.Event.WM_RBUTTONDOWN:
                return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.RButton.PressEvent, point));
            case LowLevelMouseHook.Event.WM_RBUTTONUP:
                return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.RButton.ReleaseEvent, point));
            case LowLevelMouseHook.Event.WM_MBUTTONDOWN:
                return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.MButton.PressEvent, point));
            case LowLevelMouseHook.Event.WM_MBUTTONUP:
                return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.MButton.ReleaseEvent, point));
            case LowLevelMouseHook.Event.WM_MOUSEWHEEL:
                if (data.mouseData.asWheelDelta.delta < 0)
                {
                    return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.WheelDown.FireEvent, point));
                }
                else
                {
                    return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.WheelUp.FireEvent, point));
                }
            case LowLevelMouseHook.Event.WM_XBUTTONDOWN:
                if (data.mouseData.asXButton.IsXButton1)
                {
                    return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.XButton1.PressEvent, point));
                }
                else
                {
                    return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.XButton2.PressEvent, point));
                }
            case LowLevelMouseHook.Event.WM_XBUTTONUP:
                if (data.mouseData.asXButton.IsXButton1)
                {
                    return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.XButton1.ReleaseEvent, point));
                }
                else
                {
                    return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.XButton2.ReleaseEvent, point));
                }
            case LowLevelMouseHook.Event.WM_MOUSEHWHEEL:
                if (data.mouseData.asWheelDelta.delta < 0)
                {
                    return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.WheelRight.FireEvent, point));
                }
                else
                {
                    return ToHookResult(GestureMachine.Input(SupportedKeys.PhysicalKeys.WheelLeft.FireEvent, point));
                }
        }

        return WindowsHook.Result.Transfer;
    }

    protected WindowsHook.Result ToHookResult(bool consumed)
        => consumed ? WindowsHook.Result.Cancel : WindowsHook.Result.Transfer;
}
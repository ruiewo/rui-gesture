﻿namespace RuiGesture.Models;

using System;
using System.Drawing;
using Core;
using UserScript;
using WinApi;

public class MouseGestureForm : Form
{
    private bool _hookEnabled = false;

    protected bool HookEnabled
    {
        get => _hookEnabled;
        set
        {
            if (_hookEnabled != value)
            {
                if (value)
                {
                    KeyboardHook.SetHook();
                    MouseHook.SetHook();
                    _hookEnabled = true;
                }
                else
                {
                    KeyboardHook.Unhook();
                    MouseHook.Unhook();
                    _hookEnabled = false;
                }
            }
        }
    }

    private readonly NullEvent NullEvent = new();

    public virtual IGestureMachine GestureMachine { get; }

    private readonly LowLevelKeyboardHook KeyboardHook;
    private readonly LowLevelMouseHook MouseHook;

    public MouseGestureForm()
    {
        KeyboardHook = new LowLevelKeyboardHook(KeyboardProc);
        MouseHook = new LowLevelMouseHook(MouseProc);
    }

    protected const int WM_DISPLAYCHANGE = 0x007E;

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_DISPLAYCHANGE:
                if (GestureMachine != null)
                {
                    Verbose.Print("WndProc: WM_DISPLAYCHANGE");
                    GestureMachine?.Reset();
                    Verbose.Print("GestureMachine was reset.");
                }

                break;
        }

        base.WndProc(ref m);
    }

    public WindowsHook.Result KeyboardProc(LowLevelKeyboardHook.Event evnt, LowLevelKeyboardHook.KBDLLHOOKSTRUCT data)
    {
        if (data.FromCreviceApp)
        {
            Verbose.Print($"KeyboardEvent(vkCode={data.vkCode}, " +
                          $"event={Enum.GetName(typeof(LowLevelKeyboardHook.Event), evnt)}, " +
                          $"dwExtraInfo={BitConverter.ToString(BitConverter.GetBytes((int)data.dwExtraInfo))}) " +
                          $"was passed to the next hook because this event has the signature of CreviceApp");
            return WindowsHook.Result.Transfer;
        }

        var keyCode = data.vkCode;
        if (keyCode < 8 || keyCode > 255)
        {
            return WindowsHook.Result.Transfer;
        }

        var key = SupportedKeys.PhysicalKeys[keyCode];

        switch (evnt)
        {
            case LowLevelKeyboardHook.Event.WM_KEYDOWN:
                return ToHookResult(GestureMachine.Input(key.PressEvent));

            case LowLevelKeyboardHook.Event.WM_SYSKEYDOWN:
                return ToHookResult(GestureMachine.Input(key.PressEvent));

            case LowLevelKeyboardHook.Event.WM_KEYUP:
                return ToHookResult(GestureMachine.Input(key.ReleaseEvent));

            case LowLevelKeyboardHook.Event.WM_SYSKEYUP:
                return ToHookResult(GestureMachine.Input(key.ReleaseEvent));
        }

        return WindowsHook.Result.Transfer;
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
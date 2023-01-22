namespace RuiGesture.Models.Hook;

using System;
using System.Runtime.InteropServices;
using Enums;

public class LowLevelKeyboardHook : WindowsHook
{
    public enum Event
    {
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105,
    }

    [StructLayout(LayoutKind.Sequential)]
    public class KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public FLAGS flags;
        public int time;
        public UIntPtr dwExtraInfo;

        public bool FromCreviceApp
            => ((uint)dwExtraInfo & KEYBOARDEVENTF_TMASK) == KEYBOARDEVENTF_CREVICE_APP;
    }

    [Flags]
    public enum FLAGS : int
    {
        LLKHF_EXTENDED = 0x01,
        LLKHF_INJECTED = 0x10,
        LLKHF_ALTDOWN = 0x20,
        LLKHF_UP = 0x80,
    }

    public const uint KEYBOARDEVENTF_CREVICE_APP = 0xFFFFFF00;
    public const uint KEYBOARDEVENTF_TMASK = 0xFFFFFF00;

    public LowLevelKeyboardHook(Func<Event, KBDLLHOOKSTRUCT, Result> userCallback) :
        base
        (
            HookType.WH_KEYBOARD_LL,
            (wParam, lParam) =>
            {
                var a = (Event)wParam;
                var b = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                return userCallback(a, b);
            }
        )
    {
    }
}
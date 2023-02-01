namespace RuiGesture.Models.Hook;

using System;
using System.Runtime.InteropServices;
using Enums;

public class LowLevelMouseHook : WindowsHook
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WHEELDELTA
    {
        private short lower;
        public short delta;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XBUTTON
    {
        private short lower;
        public short type;

        public bool IsXButton1 => type == 0x0001;
        public bool IsXButton2 => type == 0x0002;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MOUSEDATA
    {
        [FieldOffset(0)] public WHEELDELTA asWheelDelta;
        [FieldOffset(0)] public XBUTTON asXButton;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public MOUSEDATA mouseData;
        public int flags;
        public int time;
        public UIntPtr dwExtraInfo;

        public bool FromCreviceApp
            => ((uint)dwExtraInfo & MOUSEEVENTF_TMASK) == MOUSEEVENTF_CREVICE_APP;

        public bool FromTablet
            => ((uint)dwExtraInfo & MOUSEEVENTF_TMASK) == MOUSEEVENTF_FROMTABLET;
    }

    public const uint MOUSEEVENTF_CREVICE_APP = 0xFFFFFF00;

    public const uint MOUSEEVENTF_TMASK = 0xFFFFFF00;

    public const uint MOUSEEVENTF_FROMTABLET = 0xFF515700;

    public LowLevelMouseHook(Func<Event, MSLLHOOKSTRUCT, Result> userCallback) :
        base
        (
            HookType.WH_MOUSE_LL,
            (wParam, lParam) =>
            {
                var a = (Event)wParam;
                var b = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                return userCallback(a, b);
            }
        )
    {
    }

    public enum Event
    {
        WM_NCMOUSEMOVE = 0x00A0,
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_NCLBUTTONUP = 0x00A2,
        WM_NCLBUTTONDBLCLK = 0x00A3,
        WM_NCRBUTTONDOWN = 0x00A4,
        WM_NCRBUTTONUP = 0x00A5,
        WM_NCRBUTTONDBLCLK = 0x00A6,
        WM_NCMBUTTONDOWN = 0x00A7,
        WM_NCMBUTTONUP = 0x00A8,
        WM_NCMBUTTONDBLCLK = 0x00A9,
        WM_NCXBUTTONDOWN = 0x00AB,
        WM_NCXBUTTONUP = 0x00AC,
        WM_NCXBUTTONDBLCLK = 0x00AD,
        WM_MOUSEMOVE = 0x0200,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_RBUTTONDBLCLK = 0x0206,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MBUTTONDBLCLK = 0x0209,
        WM_MOUSEWHEEL = 0x020A,
        WM_XBUTTONDOWN = 0x020B,
        WM_XBUTTONUP = 0x020C,
        WM_XBUTTONDBLCLK = 0x020D,
        WM_MOUSEHWHEEL = 0x020E,
    }
}
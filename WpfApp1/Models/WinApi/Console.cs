namespace RuiGesture.Models.WinApi;

using System.Runtime.InteropServices;

public static class Console
{
    private static class NativeMethods
    {
        /*
         * AttachConsole is a abit buggy with CSharp GUI applictions. This bug also 
         * affects this application. If there is need to fix this issue, add a prefix 
         * before executable file path like following:
         * 
         * > cmd /c crevice4.exe
         *
         * See: Console Output from a WinForms Application : C# 411 
         * http://www.csharp411.com/console-output-from-winforms-application/
         */
        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();
    }

    public const uint ATTACH_PARENT_PROCESS = uint.MaxValue;

    public static bool AttachConsole() => NativeMethods.AttachConsole(ATTACH_PARENT_PROCESS);

    public static bool FreeConsole() => NativeMethods.FreeConsole();
}
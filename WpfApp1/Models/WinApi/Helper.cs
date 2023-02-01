namespace RuiGesture.Models.WinApi;

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

public static class ExceptionThrower
{
    public static void ThrowLastWin32Error()
    {
        throw new Win32Exception(Marshal.GetLastWin32Error());
    }
}

public class WinAPILogger
{
    private readonly StringBuilder buffer = new();

    public WinAPILogger(string name)
    {
        Add($"Calling a Win32 native API: {name}");
    }

    public void Add(string str, bool omitNewline = false)
    {
        buffer.AppendFormat(str);
        if (!omitNewline)
        {
            buffer.AppendLine();
        }
    }

    public void Success()
    {
        Add("Result: Success", true);
        Verbose.Print(buffer.ToString());
    }

    public void Fail()
    {
        Add("Result: Fail", true);
        Verbose.Print(buffer.ToString());
    }

    public void FailWithErrorCode()
    {
        Add($"Result: Fail (ErrorCode={Marshal.GetLastWin32Error()})", true);
        Verbose.Print(buffer.ToString());
    }
}
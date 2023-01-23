using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RuiGesture.Models;

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

public static class Verbose
{
    public class ElapsedTimePrinter : IDisposable
    {
        public readonly string Message;

        private readonly Stopwatch _stopwatch = new();

        public ElapsedTimePrinter(string message)
        {
            Message = $"[{message}]";
            PrintStartMessage();
            _stopwatch.Start();
        }

        private void PrintStartMessage()
        {
            Print($"{Message} was started.");
        }

        private void PrintFinishMessage()
        {
            Print($"{Message} was finished. ({_stopwatch.Elapsed})");
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
                _stopwatch.Stop();
                PrintFinishMessage();
            }
        }

        ~ElapsedTimePrinter() => Dispose(false);
    }

    public static bool Enabled { get; private set; }

    public static void Enable()
    {
        Enabled = true;
    }

    public static void Print(string message, bool omitNewline = false)
    {
        Debug.Print(message);
        if (Enabled)
        {
            try
            {
                if (omitNewline)
                {
                    Console.Write(message);
                }
                else
                {
                    Console.WriteLine(message);
                }
            }
            catch (System.IO.IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public static void Error(string message, bool omitPrefix = false, bool omitNewline = false)
    {
        var errorMessage = omitPrefix ? message : $"[Error] {message}";
        Debug.Print(errorMessage);
        if (Enabled)
        {
            if (omitNewline)
            {
                Console.Error.Write(errorMessage);
            }
            else
            {
                Console.Error.WriteLine(errorMessage);
            }
        }
    }

    public static ElapsedTimePrinter PrintElapsed(string message) => new(message);
}
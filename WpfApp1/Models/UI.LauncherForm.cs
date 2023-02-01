﻿namespace Crevice.UI;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using RuiGesture.Models;
using System.IO;
using System.Runtime.InteropServices;
using RuiGesture.Models.Config;

public partial class LauncherForm : Form
{
    private static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetForegroundWindow(HandleRef hWnd);
    }

    // Forcely make this application invisible from task switcher applications.
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle = cp.ExStyle | WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    private string _lastErrorMessage = "";

    public string LastErrorMessage
    {
        get { return _lastErrorMessage; }
        set
        {
            Verbose.Print($"LastErrorMessage was set to \"{value}\"");
            _lastErrorMessage = value;
        }
    }

    private readonly DesktopBridge.Helpers DesktopBridgeHelpers = new DesktopBridge.Helpers();

    public MainFormBase MainForm { get; set; }

    public readonly GlobalConfig Config;

    public LauncherForm(GlobalConfig config)
    {
        Config = config;
        InitializeComponent();
    }

    private static Microsoft.Win32.RegistryKey AutorunRegistry()
        => Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

    private static bool AutoRun
    {
        get
        {
            var registry = AutorunRegistry();
            try
            {
                var res = registry.GetValue(Application.ProductName);
                return res != null &&
                       (string)res == Application.ExecutablePath;
            }
            finally
            {
                registry.Close();
            }
        }
        set
        {
            if (value)
            {
                var registry = AutorunRegistry();
                registry.SetValue(Application.ProductName, Application.ExecutablePath);
                Verbose.Print("Autorun was set to true");
                registry.Close();
            }
            else
            {
                var registry = AutorunRegistry();
                if (registry.GetValue(Application.ProductName) != null)
                {
                    try
                    {
                        registry.DeleteValue(Application.ProductName);
                        Verbose.Print("Autorun was set to false");
                    }
                    catch (ArgumentException ex)
                    {
                        Verbose.Error($"An exception was thrown while writing registory value: {ex.ToString()}");
                    }
                }

                registry.Close();
            }
        }
    }

    protected override void OnShown(EventArgs e)
    {
        RegisterNotifyIcon(notifyIcon1);
        base.OnShown(e);
        MainForm.Show();
    }

    protected override void OnClosed(EventArgs e)
    {
        MainForm.Close();
        notifyIcon1.Visible = false;
        base.OnClosed(e);
    }

    protected void RegisterNotifyIcon(NotifyIcon notifyIcon)
    {
        if (!Config.CLIOption.NoGUI)
        {
            while (true)
            {
                var stopwatch = Stopwatch.StartNew();
                notifyIcon.Visible = true;
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds < 4000)
                {
                    break;
                }

                notifyIcon.Visible = false;
            }
        }
    }

    private void InvokeProperly(MethodInvoker invoker)
    {
        if (InvokeRequired)
        {
            Invoke(invoker);
        }
        else
        {
            invoker.Invoke();
        }
    }

    public void UpdateTasktrayMessage(string message)
    {
        var header = string.Format("Crevice {0}", Application.ProductVersion);
        var text = header + "\r\n" + message;
        InvokeProperly(delegate()
        {
            if (!Config.CLIOption.NoGUI)
            {
                notifyIcon1.Text = text.Length > 63 ? text.Substring(0, 60) + "..." : text;
            }
        });
    }

    public void ShowFatalErrorDialog(string text)
    {
        InvokeProperly(delegate()
        {
            Verbose.Error(text);
            MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        });
    }

    public void ShowTooltip(string text, Point point, int duration)
    {
        InvokeProperly(delegate()
        {
            Verbose.Print($"ShowTooltip: {text}");
            tooltip1.Show(text, point, duration);
        });
    }

    public void ShowBalloon(string text, string title, ToolTipIcon icon, int timeout)
    {
        InvokeProperly(delegate()
        {
            Verbose.Print($"ShowBalloon: {text}");
            if (!Config.CLIOption.NoGUI)
            {
                notifyIcon1.BalloonTipText = text;
                notifyIcon1.BalloonTipTitle = title;
                notifyIcon1.BalloonTipIcon = icon;
                notifyIcon1.ShowBalloonTip(timeout);
            }
        });
    }

    public void StartExternalProcess(string fileName)
    {
        if (!Config.CLIOption.NoGUI)
        {
            InvokeProperly(delegate()
            {
                try
                {
                    using (Verbose.PrintElapsed($"StartExternalProcess(filename={fileName})"))
                    {
                        Process.Start(fileName);
                    }
                }
                catch (Exception ex)
                {
                    Verbose.Error($"An exception was thrown while executing an external process: {ex.ToString()}");
                }
            });
        }
    }

    public void StartExternalProcess(string fileName, string arguments)
    {
        if (!Config.CLIOption.NoGUI)
        {
            InvokeProperly(delegate()
            {
                try
                {
                    using (Verbose.PrintElapsed($"StartExternalProcess(filename={fileName}, arguments={arguments})"))
                    {
                        Process.Start(fileName, arguments);
                    }
                }
                catch (Exception ex)
                {
                    Verbose.Error($"An exception was thrown while executing an external process: {ex.ToString()}");
                }
            });
        }
    }

    private void OpenWithNotepad(string path, string text)
    {
        try
        {
            File.WriteAllText(path, text);
            StartExternalProcess("notepad.exe", path);
        }
        catch (Exception ex)
        {
            Verbose.Error($"An exception was thrown while writing a file: {ex.ToString()}");
        }
    }

    public void OpenUserScriptWithExplorer()
        => StartExternalProcess("explorer.exe", "/select, " + Config.UserScriptFile);

    public void OpenLastErrorMessageWithNotepad()
    {
        if (!string.IsNullOrEmpty(LastErrorMessage))
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "Crevice4.ErrorInformation.txt");
            OpenWithNotepad(tempPath, LastErrorMessage);
        }
    }

    private void ShowProductInfoForm()
    {
        string[] args = { "--help", };
        var cliOption = CLIOption.Parse(args);
        var form = new ProductInfoForm(cliOption);
        form.Show();
    }

    private void NotifyIcon1_BalloonTipClick(object sender, EventArgs e)
    {
        OpenLastErrorMessageWithNotepad();
    }

    /// <summary>
    /// This function calcurate the preferred direction for showing <c>ContextMenuStrip</c> based on the position of the cursor.
    /// </summary>
    /// <returns>Preferred <c>ToolStripDropDownDirection</c>.</returns>
    private ToolStripDropDownDirection GetPreferredToolStripDropDownDirection()
    {
        var cursor = Cursor.Position;
        var screen = Screen.FromPoint(cursor);
        if (cursor.X < screen.Bounds.X + screen.Bounds.Width / 2)
        {
            if (cursor.Y < screen.Bounds.Y + screen.Bounds.Height / 2)
            {
                return ToolStripDropDownDirection.BelowRight;
            }
            else
            {
                return ToolStripDropDownDirection.AboveRight;
            }
        }
        else
        {
            if (cursor.Y < screen.Bounds.Y + screen.Bounds.Height / 2)
            {
                return ToolStripDropDownDirection.BelowLeft;
            }
            else
            {
                return ToolStripDropDownDirection.AboveLeft;
            }
        }
    }

    private void NotifyIcon1_MouseUp(object sender, MouseEventArgs e)
    {
        ShowContextMenu();
    }

    private void ShowContextMenu()
    {
        // `ContextMenuStrip` must be set as the foreground window before `Show()` is called, otherwise,
        // it will be shown on the top most and never to be hidden even if other window or desktop is clicked until once it is clicked.
        NativeMethods.SetForegroundWindow(new HandleRef(contextMenuStrip1, contextMenuStrip1.Handle));
        contextMenuStrip1.Show(Cursor.Position, GetPreferredToolStripDropDownDirection());
    }

    private void ClearContextMenu()
    {
        contextMenuStrip1.Items.Clear();
    }

    private void RegisterContextMenuItems0()
    {
        var item = new ToolStripMenuItem($"Crevice {Application.ProductVersion}");
        item.Click += (sender, e) => ShowProductInfoForm();
        contextMenuStrip1.Items.Add(item);
    }

    private void RegisterContextMenuItems1()
    {
        var item = new ToolStripSeparator();
        contextMenuStrip1.Items.Add(item);
    }

    private void RegisterContextMenuUwpStartup(ToolStripMenuItem item)
    {
        {
            var task = Windows.ApplicationModel.StartupTask.GetAsync("CreviceStartupTask").AsTask();
            task.Wait();
            var startupTask = task.Result;
            switch (startupTask.State)
            {
                case Windows.ApplicationModel.StartupTaskState.Disabled:
                    item.Checked = false;
                    break;
                case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                    item.Checked = false;
                    break;
                case Windows.ApplicationModel.StartupTaskState.Enabled:
                    item.Checked = true;
                    break;
            }
        }
        item.Click += async (sender, e) =>
        {
            var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("CreviceStartupTask");
            if (startupTask.State == Windows.ApplicationModel.StartupTaskState.Enabled)
            {
                Verbose.Print("CreviceStartupTask(UWP) has been disabled.");
                startupTask.Disable();
                item.Checked = false;
            }
            else
            {
                var state = await startupTask.RequestEnableAsync();
                switch (state)
                {
                    case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                        Verbose.Print("CreviceStartupTask(UWP) has been disabled by the user.");
                        item.Checked = false;
                        break;
                    case Windows.ApplicationModel.StartupTaskState.Enabled:
                        Verbose.Print("CreviceStartupTask(UWP) has been enabled.");
                        item.Checked = true;
                        break;
                }
            }
        };
    }

    private void RegisterContextMenuRegistoryStartup(ToolStripMenuItem item)
    {
        item.Checked = AutoRun;
        item.Click += (sender, e) =>
        {
            item.Checked = !AutoRun;
            AutoRun = item.Checked;
        };
    }

    /// <summary>
    /// This is a function for Windows 8 or later. Under those environment, 
    /// this function must be used instead of <c>RegisterContextMenuItems2()</c>. Otherwise, application 
    /// will cause a runtime error.
    /// </summary>
    private void RegisterContextMenuItems2()
    {
        var item = new ToolStripMenuItem("Run on Startup");
        if (DesktopBridgeHelpers.IsRunningAsUwp())
        {
            RegisterContextMenuUwpStartup(item);
        }
        else
        {
            RegisterContextMenuRegistoryStartup(item);
        }

        contextMenuStrip1.Items.Add(item);
    }

    /// <summary>
    /// This is a specialized function for Windows 7 or earlier. Under those environment, 
    /// this function must be used instead of <c>RegisterContextMenuItems2()</c>. Otherwise, application 
    /// will cause a runtime error.
    /// </summary>
    private void RegisterContextMenuItems2_Win61()
    {
        var item = new ToolStripMenuItem("Run on Startup");
        RegisterContextMenuRegistoryStartup(item);
        contextMenuStrip1.Items.Add(item);
    }

    private void RegisterContextMenuItems3()
    {
        var item = new ToolStripSeparator();
        contextMenuStrip1.Items.Add(item);
    }

    private void RegisterContextMenuItems4()
    {
        if (!string.IsNullOrEmpty(LastErrorMessage))
        {
            var item = new ToolStripMenuItem("View ErrorMessage");
            item.Click += (sender, e) => OpenLastErrorMessageWithNotepad();
            contextMenuStrip1.Items.Add(item);
        }
    }

    private void RegisterContextMenuItems5()
    {
        var item = new ToolStripMenuItem("Open Documentation");
        item.Click += (sender, e) => StartExternalProcess("https://creviceapp.github.io");
        contextMenuStrip1.Items.Add(item);
    }

    private void RegisterContextMenuItems6()
    {
        var item = new ToolStripMenuItem("Open UserScript");
        item.Click += (sender, e) => OpenUserScriptWithExplorer();
        contextMenuStrip1.Items.Add(item);
    }

    private void RegisterContextMenuItems7()
    {
        var item = new ToolStripSeparator();
        contextMenuStrip1.Items.Add(item);
    }

    private void RegisterContextMenuItems8()
    {
        var item = new ToolStripMenuItem("Exit");
        item.Click += (sender, e) =>
        {
            Close();
            Application.ExitThread();
            Process.GetCurrentProcess().CloseMainWindow();
        };
        contextMenuStrip1.Items.Add(item);
    }

    private void ResetContextMenu()
    {
        ClearContextMenu();
        RegisterContextMenuItems0();
        RegisterContextMenuItems1();
        RegisterContextMenuItems2();
        RegisterContextMenuItems3();
        RegisterContextMenuItems4();
        RegisterContextMenuItems5();
        RegisterContextMenuItems6();
        RegisterContextMenuItems7();
        RegisterContextMenuItems8();
    }

    private void ContextMenu1_Opening(object sender, CancelEventArgs e)
    {
        ResetContextMenu();
    }

    private void ResetContextMenu_Win61()
    {
        ClearContextMenu();
        RegisterContextMenuItems0();
        RegisterContextMenuItems1();
        RegisterContextMenuItems2_Win61();
        RegisterContextMenuItems3();
        RegisterContextMenuItems4();
        RegisterContextMenuItems5();
        RegisterContextMenuItems6();
        RegisterContextMenuItems7();
        RegisterContextMenuItems8();
    }

    private void ContextMenu1_Opening_Win61(object sender, CancelEventArgs e)
    {
        ResetContextMenu_Win61();
    }

    private bool IsWin7OrLower =>
        Environment.OSVersion.Version.Major < 6 ||
        Environment.OSVersion.Version.Major == 6 &&
        Environment.OSVersion.Version.Minor <= 1;
}
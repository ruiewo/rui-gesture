using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RuiGesture.Models;

namespace Crevice.UI;

using Core.FSM;
using UserScript;
using RuiGesture.Models.Gesture;
using RuiGesture.Models.Core;
using RuiGesture.Models.UserScript;

public partial class ReloadableMainForm : MainFormBase
{
    private readonly FileSystemWatcher _userScriptWatcher;

    internal readonly ReloadableGestureMachine _reloadableGestureMachine;
    public override IGestureMachine GestureMachine => _reloadableGestureMachine;

    public ReloadableMainForm(LauncherForm launcherForm)
        : base(launcherForm)
    {
        _reloadableGestureMachine = new ReloadableGestureMachine(launcherForm.Config, this);
        _userScriptWatcher = new UserScript.DirectoryWatcher(this, launcherForm.Config.UserDirectory, "*.csx");
        InitializeComponent();
    }

    private void ReloadGestureMachine(object source, FileSystemEventArgs e)
        => Invoke((MethodInvoker)(() =>
        {
            Verbose.Print($"UserScript.DirectoryWatcher called ReloadGestureMachine()");
            Verbose.Print($"ChangeType={e.ChangeType}, Path={e.FullPath}");
            Task.Factory.StartNew(() => { _reloadableGestureMachine.HotReload(); });
        }));

    private void SetupUserScriptWatcher()
    {
        _userScriptWatcher.Changed += new FileSystemEventHandler(ReloadGestureMachine);
        _userScriptWatcher.Created += new FileSystemEventHandler(ReloadGestureMachine);
        _userScriptWatcher.Renamed += new RenamedEventHandler(ReloadGestureMachine);
        _userScriptWatcher.EnableRaisingEvents = true;
    }

    protected override void OnShown(EventArgs e)
    {
        Verbose.Print("CreviceApp was started.");
        base.OnShown(e);
        _reloadableGestureMachine.HotReload();
        SetupUserScriptWatcher();
    }

    protected override void OnClosed(EventArgs e)
    {
        _userScriptWatcher.EnableRaisingEvents = false;
        base.OnClosed(e);
        Verbose.Print("CreviceApp was ended.");
    }
}
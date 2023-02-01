namespace Crevice.UI;

using System;
using System.Collections.Generic;
using RuiGesture.Models;
using RuiGesture.Models.Core;
using RuiGesture.Models.Gesture;

public partial class ClusterMainForm : MainFormBase
{
    internal GestureMachineCluster _gestureMachineCluster = null;
    public override IGestureMachine GestureMachine => _gestureMachineCluster;

    public ClusterMainForm(LauncherForm launcherForm)
        : base(launcherForm)
    {
        InitializeComponent();
    }

    public void Run(IReadOnlyList<GestureMachineProfile> gestureMachineProfiles)
    {
        if (_gestureMachineCluster != null)
        {
            throw new InvalidOperationException();
        }

        _gestureMachineCluster = new GestureMachineCluster(gestureMachineProfiles);
        _gestureMachineCluster.Run();
    }

    protected override void OnShown(EventArgs e)
    {
        if (GestureMachine == null)
        {
            throw new InvalidOperationException();
        }

        Verbose.Print("CreviceApp was started.");
        UpdateTasktrayMessage(_gestureMachineCluster.Profiles);
        ShowInfoBalloon(_gestureMachineCluster);
        base.OnShown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _gestureMachineCluster.Stop();
        Verbose.Print("CreviceApp was ended.");
        base.OnClosed(e);
    }
}
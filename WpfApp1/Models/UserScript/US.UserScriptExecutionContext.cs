namespace RuiGesture.Models.UserScript;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Config;
using Gesture;
using Core;
using WinApi;

public class UserScriptExecutionContext
{
    private readonly GestureMachineProfileManager ProfileManager = new();

    public GestureMachineProfile CurrentProfile
    {
        get
        {
            if (!ProfileManager.Profiles.Any())
            {
                DeclareProfile("Default");
            }

            return ProfileManager.Profiles.Last();
        }
    }

    public void DeclareProfile(string profileName)
        => ProfileManager.DeclareProfile(profileName);

    public IReadOnlyList<GestureMachineProfile> Profiles
        => ProfileManager.Profiles;

    public readonly SupportedKeys.LogicalKeyDeclaration Keys = SupportedKeys.Keys;

    #region Crevice3 compatible variables.

    public LogicalSingleThrowKey WheelDown => Keys.WheelDown;
    public LogicalSingleThrowKey WheelUp => Keys.WheelUp;
    public LogicalSingleThrowKey WheelLeft => Keys.WheelLeft;
    public LogicalSingleThrowKey WheelRight => Keys.WheelRight;

    public LogicalSystemKey LeftButton => Keys.LButton;
    public LogicalSystemKey MiddleButton => Keys.MButton;
    public LogicalSystemKey RightButton => Keys.RButton;
    public LogicalSystemKey X1Button => Keys.XButton1;
    public LogicalSystemKey X2Button => Keys.XButton2;

    public StrokeDirection MoveUp => Keys.MoveUp;
    public StrokeDirection MoveDown => Keys.MoveDown;
    public StrokeDirection MoveLeft => Keys.MoveLeft;
    public StrokeDirection MoveRight => Keys.MoveRight;

    #endregion

    public readonly IReadOnlyList<SupportedKeys.PhysicalKeyDeclaration> PhysicalKeys =
        new List<SupportedKeys.PhysicalKeyDeclaration>() { SupportedKeys.PhysicalKeys, };

    public readonly SingleInputSender SendInput = new();

    private readonly GlobalConfig _config;
    private readonly MainFormBase _mainForm;

    public UserScriptExecutionContext(GlobalConfig config, MainFormBase mainForm)
    {
        _config = config;
        _mainForm = mainForm;
    }

    public void InvokeOnMainThread(Action action)
        => _mainForm.BeginInvoke(action);

    public IGestureMachine RootGestureMachine
        => _mainForm.GestureMachine;

    public UserConfig Config
        => CurrentProfile.UserConfig;

    public WhenElement
        When(EvaluateAction func)
        => CurrentProfile.RootElement.When(func);

    public void Tooltip(string text)
        => Tooltip(text, Config.UI.TooltipPositionBinding(Window.GetPhysicalCursorPos()));

    public void Tooltip(string text, Point point)
        => Tooltip(text, point, Config.UI.TooltipTimeout);

    public void Tooltip(string text, Point point, int duration)
        => _mainForm.ShowTooltip(text, point, duration);

    public void Balloon(string text)
        => Balloon(text, Config.UI.BalloonTimeout);

    public void Balloon(string text, int timeout)
        => _mainForm.ShowBalloon(text, "", ToolTipIcon.None, timeout);

    public void Balloon(string text, string title)
        => Balloon(text, title, ToolTipIcon.None, Config.UI.BalloonTimeout);

    public void Balloon(string text, string title, int timeout)
        => _mainForm.ShowBalloon(text, title, ToolTipIcon.None, timeout);

    public void Balloon(string text, string title, ToolTipIcon icon)
        => Balloon(text, title, icon, Config.UI.BalloonTimeout);

    public void Balloon(string text, string title, ToolTipIcon icon, int timeout)
        => _mainForm.ShowBalloon(text, title, icon, timeout);
}
﻿namespace RuiGesture.Models.Gesture;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using WinApi;
using UserScript;

public class CallbackManager : CallbackManager<ContextManager>,
    IDisposable
{
    public class ActionExecutor : IDisposable
    {
        private readonly LowLatencyScheduler _scheduler;
        private readonly TaskFactory _taskFactory;

        public ActionExecutor(string name, ThreadPriority threadPriority, int poolSize)
        {
            _scheduler = new LowLatencyScheduler($"{name}Scheduler", threadPriority, poolSize);
            _taskFactory = new TaskFactory(_scheduler);
        }

        public void Execute(Action action) => _taskFactory.StartNew(action);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scheduler.Dispose();
            }
        }
    }

    public class CustomCallbackContainer : CallbackContainer
    {
        private readonly ActionExecutor _actionExecutor;

        public CustomCallbackContainer(ActionExecutor actionExecutor)
        {
            _actionExecutor = actionExecutor;
        }

        protected override void Invoke(Action action) => _actionExecutor.Execute(action);
    }

    private static ActionExecutor CallbackActionExecutor => new("CallbackActionExecutor", ThreadPriority.Highest,
        Math.Max(2, Environment.ProcessorCount / 2));

    private static ActionExecutor SystemKeyRestorationActionExecutor =>
        new("SystemKeyRestorationActionExecutor", ThreadPriority.Highest, 1);

    private readonly ActionExecutor _callbackActionExecutor;
    private readonly ActionExecutor _systemKeyRestorationActionExecutor;

    private readonly SingleInputSender SingleInputSender = new();

    public CallbackManager() : this(CallbackActionExecutor)
    {
    }

    public CallbackManager(ActionExecutor callbackActionExecutor) : base(
        new CustomCallbackContainer(callbackActionExecutor))
    {
        _callbackActionExecutor = callbackActionExecutor;
        _systemKeyRestorationActionExecutor = SystemKeyRestorationActionExecutor;
    }

    public override void OnStrokeReset(
        GestureMachine<ContextManager> gestureMachine,
        StrokeWatcher lastStrokeWatcher,
        StrokeWatcher currentStrokeWatcher)
    {
        Verbose.Print("Stroke was reset.");
        OnStrokeReset(gestureMachine, lastStrokeWatcher, currentStrokeWatcher);
    }

    public override void OnStrokeUpdated(
        StrokeWatcher strokeWatcher,
        IReadOnlyList<Stroke> strokes)
    {
        var strokeString = strokes
            .Select(s =>
            {
                if (s.Direction == StrokeDirection.Up)
                {
                    return "↑";
                }

                if (s.Direction == StrokeDirection.Down)
                {
                    return "↓";
                }

                if (s.Direction == StrokeDirection.Left)
                {
                    return "←";
                }

                /*if (s.Direction == StrokeDirection.Right)*/
                return "→";
            })
            .Aggregate("", (a, b) => a + b);
        var strokePoints =
            strokes
                .Select(s => s.Points.Count.ToString())
                .Aggregate((a, b) => a + ", " + b);
        Verbose.Print($"Stroke was updated; Directions={strokeString}, Points={strokePoints}");
        OnStrokeUpdated(strokeWatcher, strokes);
    }

    internal HashSet<PhysicalSystemKey> TimeoutKeyboardKeys { get; private set; } = new();

    public override void OnStateChanged(
        GestureMachine<ContextManager> gestureMachine,
        State<ContextManager> lastState,
        State<ContextManager> currentState)
    {
        Verbose.Print($"State was changed; CurrentState={currentState}");
        if (lastState != null && lastState.IsState0 && currentState.IsStateN)
        {
            Verbose.Print("TimeoutKeyboardKeys was cleared.");
            TimeoutKeyboardKeys.Clear();
        }

        OnStateChanged(gestureMachine, lastState, currentState);
    }

    public override void OnGestureCanceled(
        GestureMachine<ContextManager> gestureMachine,
        StateN<ContextManager> stateN)
    {
        Verbose.Print("Gesture was cancelled.");
        var systemKeys = stateN.History.Records.Select(r => r.ReleaseEvent.PhysicalKey as PhysicalSystemKey);
        Verbose.Print($"Current keys: {string.Join(", ", systemKeys)}");
        _systemKeyRestorationActionExecutor.Execute(() => { RestoreKeyPressAndReleaseEvents(systemKeys); });
        OnGestureCanceled(gestureMachine, stateN);
    }

    public override void OnGestureTimeout(
        GestureMachine<ContextManager> gestureMachine,
        StateN<ContextManager> stateN)
    {
        Verbose.Print("Gesture was timeout.");
        var systemKeys = stateN.History.Records.Select(r => r.ReleaseEvent.PhysicalKey as PhysicalSystemKey);
        Verbose.Print($"Current keys: {string.Join(", ", systemKeys)}");

        _systemKeyRestorationActionExecutor.Execute(() => { RestoreKeyPressEvents(systemKeys); });

        foreach (var key in systemKeys.Where(key => key.IsKeyboardKey))
        {
            Verbose.Print($"{key} was added to TimeoutKeyboardKeys.");
            TimeoutKeyboardKeys.Add(key);
        }

        OnGestureTimeout(gestureMachine, stateN);
    }

    public override void OnMachineReset(
        GestureMachine<ContextManager> gestureMachine,
        State<ContextManager> state)
    {
        Verbose.Print($"GestureMachine was reset; LastState={state}");
        OnMachineReset(gestureMachine, state);
    }

    public override void OnMachineStart(GestureMachine<ContextManager> gestureMachine)
    {
        Verbose.Print("GestureMachine was started.");
        OnMachineStart(gestureMachine);
    }

    public override void OnMachineStop(GestureMachine<ContextManager> gestureMachine)
    {
        Verbose.Print("GestureMachine was stopped.");
        OnMachineStop(gestureMachine);
    }

    internal void RestoreKeyPressEvents(IEnumerable<PhysicalSystemKey> systemKeys)
    {
        foreach (var systemKey in systemKeys)
        {
            if (systemKey == SupportedKeys.PhysicalKeys.None)
            {
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.LButton)
            {
                SingleInputSender.LeftDown();
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.RButton)
            {
                SingleInputSender.RightDown();
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.MButton)
            {
                SingleInputSender.MiddleDown();
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.XButton1)
            {
                SingleInputSender.X1Down();
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.XButton2)
            {
                SingleInputSender.X2Down();
            }
            else
            {
                SingleInputSender.KeyDownWithScanCode(systemKey.KeyId);
            }
        }
    }

    internal void RestoreKeyPressAndReleaseEvents(IEnumerable<PhysicalSystemKey> systemKeys)
    {
        foreach (var systemKey in systemKeys)
        {
            if (systemKey == SupportedKeys.PhysicalKeys.None)
            {
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.LButton)
            {
                SingleInputSender.LeftClick();
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.RButton)
            {
                SingleInputSender.RightClick();
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.MButton)
            {
                SingleInputSender.MiddleClick();
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.XButton1)
            {
                SingleInputSender.X1Click();
            }
            else if (systemKey == SupportedKeys.PhysicalKeys.XButton2)
            {
                SingleInputSender.X2Click();
            }
            else
            {
                SingleInputSender.Multiple()
                    .KeyDownWithScanCode(systemKey.KeyId)
                    .KeyUpWithScanCode(systemKey.KeyId)
                    .Send();
            }
        }
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
            _callbackActionExecutor.Dispose();
            _systemKeyRestorationActionExecutor.Dispose();
        }
    }
}
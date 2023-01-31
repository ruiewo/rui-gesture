namespace RuiGesture.Models.Gesture;

using System.Collections.Generic;
using DSL;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Config;
using Event;

public class GestureMachine : GestureMachineAbs
{
    private readonly CallbackManagerImpl customCallbackManager;

    public GestureMachine(CallbackManagerImpl callbackManager) : base(callbackManager, new ContextManager())
    {
        customCallbackManager = callbackManager;
    }

    private readonly LowLatencyScheduler strokeWatcherScheduler =
        new("StrokeWatcherTaskScheduler", ThreadPriority.Highest, 2);

    protected internal override TaskFactory StrokeWatcherTaskFactory => new(strokeWatcherScheduler);

    public override bool Input(IPhysicalEvent evnt, Point? point)
    {
        lock (lockObject)
        {
            if (point.HasValue)
            {
                ContextManager.CursorPosition = point.Value;
            }

            if (evnt is PhysicalPressEvent pressEvent)
            {
                var key = pressEvent.PhysicalKey as PhysicalSystemKey;
                if (key.IsKeyboardKey && customCallbackManager.TimeoutKeyboardKeys.Contains(key))
                {
                    return false;
                }
            }
            else if (evnt is PhysicalReleaseEvent releaseEvent)
            {
                var key = releaseEvent.PhysicalKey as PhysicalSystemKey;
                if (key.IsKeyboardKey && customCallbackManager.TimeoutKeyboardKeys.Contains(key))
                {
                    customCallbackManager.TimeoutKeyboardKeys.Remove(key);
                    return false;
                }
            }

            return base.Input(evnt, point);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ContextManager.Dispose();
            strokeWatcherScheduler.Dispose();
        }

        base.Dispose(disposing);
    }
}

public abstract class GestureMachineAbs : IDisposable
{
    public readonly CallbackManager CallbackManager;
    public readonly ContextManager ContextManager;

    protected readonly object lockObject = new();

    private readonly System.Timers.Timer gestureTimeoutTimer = new();

    internal readonly EventCounter<PhysicalReleaseEvent> invalidEvents = new();

    public IReadOnlyRootElement RootElement { get; internal set; }

    public StrokeWatcher StrokeWatcher { get; internal set; }

    private State _currentState = null;

    public State CurrentState
    {
        get => _currentState;

        internal set
        {
            if (_currentState != value)
            {
                ResetStrokeWatcher();
                if (value is State0)
                {
                    StopGestureTimeoutTimer();
                }
                else if (value is StateN)
                {
                    ResetGestureTimeoutTimer();
                }

                var lastState = _currentState;
                _currentState = value;
                CallbackManager.OnStateChanged(this, lastState, _currentState);
            }
        }
    }

    protected internal virtual TaskFactory StrokeWatcherTaskFactory => Task.Factory;

    public GestureMachineAbs(CallbackManager callbackManager, ContextManager contextManager)
    {
        CallbackManager = callbackManager;
        ContextManager = contextManager;

        SetupGestureTimeoutTimer();
    }

    public bool IsRunning { get; internal set; } = false;

    public virtual void Run(IReadOnlyRootElement rootElement)
    {
        lock (lockObject)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException();
            }

            CurrentState = new State0(this, rootElement);
            RootElement = rootElement;
            IsRunning = true;
            CallbackManager.OnMachineStart(this);
        }
    }

    public virtual bool Input(IPhysicalEvent evnt) => Input(evnt, null);

    public virtual bool Input(IPhysicalEvent evnt, Point? point)
    {
        lock (lockObject)
        {
            if (point.HasValue && CurrentState is StateN)
            {
                StrokeWatcher.Process(point.Value);
            }

            if (evnt is NullEvent)
            {
                return false;
            }
            else if (evnt is PhysicalReleaseEvent releaseEvent && invalidEvents[releaseEvent] > 0)
            {
                invalidEvents.CountDown(releaseEvent);
                return true;
            }

            var result = CurrentState.Input(evnt);
            CurrentState = result.NextState;
            return result.EventIsConsumed;
        }
    }

    private void SetupGestureTimeoutTimer()
    {
        gestureTimeoutTimer.Elapsed += new System.Timers.ElapsedEventHandler(TryTimeout);
        gestureTimeoutTimer.AutoReset = false;
    }

    private void StopGestureTimeoutTimer()
    {
        gestureTimeoutTimer.Stop();
    }

    private void ResetGestureTimeoutTimer()
    {
        gestureTimeoutTimer.Stop();
        if (GestureConfig.GestureTimeout > 0)
        {
            gestureTimeoutTimer.Interval = GestureConfig.GestureTimeout;
            gestureTimeoutTimer.Start();
        }
    }

    private StrokeWatcher CreateStrokeWatcher()
        => new(
            CallbackManager,
            StrokeWatcherTaskFactory,
            GestureConfig.StrokeStartThreshold,
            GestureConfig.StrokeDirectionChangeThreshold,
            GestureConfig.StrokeExtensionThreshold,
            GestureConfig.StrokeWatchInterval);

    private void ResetStrokeWatcher()
    {
        var old = StrokeWatcher;
        StrokeWatcher = CreateStrokeWatcher();
        old?.Dispose();
        CallbackManager.OnStrokeReset(this, old, StrokeWatcher);
    }

    private void TryTimeout(object sender, System.Timers.ElapsedEventArgs args)
    {
        lock (lockObject)
        {
            if (CurrentState is StateN lastState)
            {
                var state = CurrentState;
                var _state = CurrentState.Timeout();
                while (state != _state)
                {
                    state = _state;
                    _state = state.Timeout();
                }

                if (CurrentState != state)
                {
                    CurrentState = state;
                    CallbackManager.OnGestureTimeout(this, lastState);
                }
            }
        }
    }

    public void Reset()
    {
        lock (lockObject)
        {
            var lastState = CurrentState;
            if (CurrentState is StateN)
            {
                var state = CurrentState;
                var _state = CurrentState.Reset();
                while (state != _state)
                {
                    state = _state;
                    _state = state.Reset();
                }

                CurrentState = state;
            }

            CallbackManager.OnMachineReset(this, lastState);
        }
    }

    public void Stop()
    {
        Reset();
        CallbackManager.OnMachineStop(this);
    }

    internal bool _disposed { get; private set; } = false;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            gestureTimeoutTimer.Dispose();
            StrokeWatcher?.Dispose();
        }
    }

    ~GestureMachineAbs() => Dispose(false);
}

public class EventCounter<TEvent>
    where TEvent : Event
{
    public class NaturalNumberCounter<T>
    {
        private readonly Dictionary<T, int> Dictionary = new();

        public int this[T key]
        {
            get { return Dictionary.TryGetValue(key, out var count) ? count : 0; }
            set
            {
                if (value < 0)
                {
                    throw new InvalidOperationException("n >= 0");
                }

                Dictionary[key] = value;
            }
        }

        public void CountDown(T key)
        {
            Dictionary[key] = this[key] - 1;
        }

        public void CountUp(T key)
        {
            Dictionary[key] = this[key] + 1;
        }
    }

    private readonly NaturalNumberCounter<TEvent> InvalidReleaseEvents = new();

    public int this[TEvent key]
    {
        get => InvalidReleaseEvents[key];
    }

    public void IgnoreNext(TEvent releaseEvent) => InvalidReleaseEvents.CountUp(releaseEvent);

    public void IgnoreNext(IEnumerable<TEvent> releaseEvents)
    {
        foreach (var releaseEvent in releaseEvents)
        {
            IgnoreNext(releaseEvent);
        }
    }

    public void CountDown(TEvent key) => InvalidReleaseEvents.CountDown(key);
}
namespace RuiGesture.Models.Core;

using System;
using System.Drawing;
using System.Threading.Tasks;
using Config;

public interface IGestureMachine
{
    bool Input(IPhysicalEvent evnt);

    bool Input(IPhysicalEvent evnt, Point? point);

    void Reset();
}

public abstract class GestureMachine<TContextManager> : IGestureMachine, IDisposable
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public readonly GestureMachineConfig Config;
    public readonly CallbackManager<TContextManager> CallbackManager;
    public readonly TContextManager ContextManager;

    protected readonly object lockObject = new();

    private readonly System.Timers.Timer gestureTimeoutTimer = new();

    internal readonly EventCounter<PhysicalReleaseEvent> invalidEvents = new();

    public IReadOnlyRootElement RootElement { get; internal set; }

    public StrokeWatcher StrokeWatcher { get; internal set; }

    private State<TContextManager> currentState = null;

    public State<TContextManager> CurrentState
    {
        get => currentState;

        internal set
        {
            if (currentState != value)
            {
                ResetStrokeWatcher();
                if (value is State0<TContextManager>)
                {
                    StopGestureTimeoutTimer();
                }
                else if (value is StateN<TContextManager>)
                {
                    ResetGestureTimeoutTimer();
                }

                var lastState = currentState;
                currentState = value;
                CallbackManager.OnStateChanged(this, lastState, currentState);
            }
        }
    }

    protected internal virtual TaskFactory StrokeWatcherTaskFactory => Task.Factory;

    public GestureMachine(
        GestureMachineConfig config,
        CallbackManager<TContextManager> callbackManager,
        TContextManager contextManager)
    {
        Config = config;
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

            CurrentState = new State0<TContextManager>(this, rootElement);
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
            if (point.HasValue && CurrentState is StateN<TContextManager>)
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
        if (Config.GestureTimeout > 0)
        {
            gestureTimeoutTimer.Interval = Config.GestureTimeout;
            gestureTimeoutTimer.Start();
        }
    }

    private StrokeWatcher CreateStrokeWatcher()
        => new(
            CallbackManager,
            StrokeWatcherTaskFactory,
            Config.StrokeStartThreshold,
            Config.StrokeDirectionChangeThreshold,
            Config.StrokeExtensionThreshold,
            Config.StrokeWatchInterval);

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
            if (CurrentState is StateN<TContextManager> lastState)
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
            if (CurrentState is StateN<TContextManager>)
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

    ~GestureMachine() => Dispose(false);
}
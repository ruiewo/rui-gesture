namespace RuiGesture.Models.Core;

using System;
using System.Collections.Generic;

public interface IStrokeCallbackManager
{
    void OnStrokeUpdated(StrokeWatcher strokeWatcher, IReadOnlyList<Stroke> strokes);
}

public class CallbackManager<TContextManager> : IStrokeCallbackManager
    where TContextManager : ContextManager<EvaluationContext, ExecutionContext>
{
    public class CallbackContainer
    {
        protected virtual void Invoke(Action action) => action();

        public event StrokeResetEventHandler StrokeReset;

        public virtual void OnStrokeReset(object o, StrokeResetEventArgs e)
            => Invoke(() => { StrokeReset?.Invoke(o, e); });

        public event StrokeUpdatedEventHandler StrokeUpdated;

        public virtual void OnStrokeUpdated(object o, StrokeUpdatedEventArgs e)
            => Invoke(() => { StrokeUpdated?.Invoke(o, e); });

        public event StateChangedEventHandler StateChanged;

        public virtual void OnStateChanged(object o, StateChangedEventArgs e)
            => Invoke(() => { StateChanged?.Invoke(o, e); });

        public event GestureCanceledEventHandler GestureCanceled;

        public virtual void OnGestureCanceled(object o, GestureCanceledEventArgs e)
            => Invoke(() => { GestureCanceled?.Invoke(o, e); });

        public event GestureTimeoutEventHandler GestureTimeout;

        public virtual void OnGestureTimeout(object o, GestureTimeoutEventArgs e)
            => Invoke(() => { GestureTimeout?.Invoke(o, e); });

        public event MachineResetEventHandler MachineReset;

        public virtual void OnMachineReset(object o, MachineResetEventArgs e)
            => Invoke(() => { MachineReset?.Invoke(o, e); });

        public event MachineStartEventHandler MachineStart;

        public virtual void OnMachineStart(object o, MachineStartEventArgs e)
            => Invoke(() => { MachineStart?.Invoke(o, e); });

        public event MachineStopEventHandler MachineStop;

        public virtual void OnMachineStop(object o, MachineStopEventArgs e)
            => Invoke(() => { MachineStop?.Invoke(o, e); });
    }

    public readonly CallbackContainer Callback;

    public CallbackManager() : this(new CallbackContainer())
    {
    }

    public CallbackManager(CallbackContainer callback)
    {
        Callback = callback;
    }

    #region Event StrokeReset

    public class StrokeResetEventArgs : EventArgs
    {
        public readonly StrokeWatcher LastStrokeWatcher;
        public readonly StrokeWatcher CurrentStrokeWatcher;

        public StrokeResetEventArgs(StrokeWatcher lastStrokeWatcher, StrokeWatcher currentStrokeWatcher)
        {
            LastStrokeWatcher = lastStrokeWatcher;
            CurrentStrokeWatcher = currentStrokeWatcher;
        }
    }

    public delegate void StrokeResetEventHandler(object sender, StrokeResetEventArgs e);

    public virtual void OnStrokeReset(
        GestureMachine<TContextManager> gestureMachine,
        StrokeWatcher lastStrokeWatcher,
        StrokeWatcher currentStrokeWatcher)
        => Callback.OnStrokeReset(gestureMachine, new StrokeResetEventArgs(lastStrokeWatcher, currentStrokeWatcher));

    #endregion

    #region Event StrokeUpdated

    public class StrokeUpdatedEventArgs : EventArgs
    {
        public readonly IReadOnlyList<Stroke> Strokes;

        public StrokeUpdatedEventArgs(IReadOnlyList<Stroke> strokes)
        {
            Strokes = strokes;
        }
    }

    public delegate void StrokeUpdatedEventHandler(object sender, StrokeUpdatedEventArgs e);

    public virtual void OnStrokeUpdated(StrokeWatcher strokeWatcher, IReadOnlyList<Stroke> strokes)
        => Callback.OnStrokeUpdated(strokeWatcher, new StrokeUpdatedEventArgs(strokes));

    #endregion

    #region Event StateChanged

    public class StateChangedEventArgs : EventArgs
    {
        public readonly State<TContextManager> LastState;
        public readonly State<TContextManager> CurrentState;

        public StateChangedEventArgs(
            State<TContextManager> lastState,
            State<TContextManager> currentState)
        {
            LastState = lastState;
            CurrentState = currentState;
        }
    }

    public delegate void StateChangedEventHandler(object sender, StateChangedEventArgs e);

    public virtual void OnStateChanged(
        GestureMachine<TContextManager> gestureMachine,
        State<TContextManager> lastState,
        State<TContextManager> currentState)
        => Callback.OnStateChanged(gestureMachine, new StateChangedEventArgs(lastState, currentState));

    #endregion

    #region Event GestureCanceled

    public class GestureCanceledEventArgs : EventArgs
    {
        public readonly StateN<TContextManager> LastState;

        public GestureCanceledEventArgs(StateN<TContextManager> stateN)
        {
            LastState = stateN;
        }
    }

    public delegate void GestureCanceledEventHandler(object sender, GestureCanceledEventArgs e);

    public virtual void OnGestureCanceled(
        GestureMachine<TContextManager> gestureMachine,
        StateN<TContextManager> stateN)
        => Callback.OnGestureCanceled(gestureMachine, new GestureCanceledEventArgs(stateN));

    #endregion

    #region Event GestureTimeout

    public class GestureTimeoutEventArgs : EventArgs
    {
        public readonly StateN<TContextManager> LastState;

        public GestureTimeoutEventArgs(StateN<TContextManager> stateN)
        {
            LastState = stateN;
        }
    }

    public delegate void GestureTimeoutEventHandler(object sender, GestureTimeoutEventArgs e);

    public virtual void OnGestureTimeout(
        GestureMachine<TContextManager> gestureMachine,
        StateN<TContextManager> stateN)
        => Callback.OnGestureTimeout(gestureMachine, new GestureTimeoutEventArgs(stateN));

    #endregion

    #region Event MachineReset

    public class MachineResetEventArgs : EventArgs
    {
        public readonly State<TContextManager> LastState;

        public MachineResetEventArgs(State<TContextManager> lastState)
        {
            LastState = lastState;
        }
    }

    public delegate void MachineResetEventHandler(object sender, MachineResetEventArgs e);

    public virtual void OnMachineReset(
        GestureMachine<TContextManager> gestureMachine,
        State<TContextManager> state)
        => Callback.OnMachineReset(gestureMachine, new MachineResetEventArgs(state));

    #endregion

    #region Event MachineStart

    public class MachineStartEventArgs : EventArgs
    {
    }

    public delegate void MachineStartEventHandler(object sender, MachineStartEventArgs e);

    public virtual void OnMachineStart(
        GestureMachine<TContextManager> gestureMachine)
        => Callback.OnMachineStart(gestureMachine, new MachineStartEventArgs());

    #endregion

    #region Event MachineStop

    public class MachineStopEventArgs : EventArgs
    {
    }

    public delegate void MachineStopEventHandler(object sender, MachineStopEventArgs e);

    public virtual void OnMachineStop(
        GestureMachine<TContextManager> gestureMachine)
        => Callback.OnMachineStop(gestureMachine, new MachineStopEventArgs());

    #endregion
}
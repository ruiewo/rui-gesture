namespace RuiGesture.Models.Gesture;

using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using Core;
using UserScript;

public class GestureMachine : GestureMachine<GestureMachineConfig, ContextManager>
{
    private readonly CallbackManager customCallbackManager;


    public GestureMachine(
        GestureMachineConfig gestureMachineConfig,
        CallbackManager callbackManager)
        : base(gestureMachineConfig, callbackManager, new ContextManager())
    {
        customCallbackManager = callbackManager;
    }

    private readonly LowLatencyScheduler _strokeWatcherScheduler =
        new("StrokeWatcherTaskScheduler", ThreadPriority.Highest, 2);

    protected internal override TaskFactory StrokeWatcherTaskFactory => new(_strokeWatcherScheduler);

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

            return Input(evnt, point);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ContextManager.Dispose();
            _strokeWatcherScheduler.Dispose();
        }

        Dispose(disposing);
    }
}
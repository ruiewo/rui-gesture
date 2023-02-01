namespace RuiGesture.Models.Gesture;

using System;
using System.Collections.Generic;
using Core;

public class GestureMachineCluster : IGestureMachine, IDisposable
{
    public readonly IReadOnlyList<GestureMachineProfile> Profiles;

    public GestureMachineCluster(IReadOnlyList<GestureMachineProfile> profiles)
    {
        Profiles = profiles;
    }

    public void Run()
    {
        foreach (var profile in Profiles)
        {
            profile.GestureMachine.Run(profile.RootElement);
        }
    }

    public bool Input(IPhysicalEvent physicalEvent, System.Drawing.Point? point)
    {
        foreach (var profile in Profiles)
        {
            var eventIsConsumed = profile.GestureMachine.Input(physicalEvent, point);
            if (eventIsConsumed == true)
            {
                return true;
            }
        }

        return false;
    }

    public bool Input(IPhysicalEvent physicalEvent)
        => Input(physicalEvent, null);

    public void Reset()
    {
        foreach (var profile in Profiles)
        {
            profile.GestureMachine.Reset();
        }
    }

    public void Stop()
    {
        foreach (var profile in Profiles)
        {
            profile.GestureMachine.Stop();
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
            foreach (var profile in Profiles)
            {
                profile.Dispose();
            }
        }
    }

    ~GestureMachineCluster() => Dispose(false);
}
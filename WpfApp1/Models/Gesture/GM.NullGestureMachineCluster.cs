namespace RuiGesture.Models.Gesture;

using System.Collections.Generic;

public class NullGestureMachineCluster : GestureMachineCluster
{
    public NullGestureMachineCluster() : base(new List<GestureMachineProfile>())
    {
    }
}
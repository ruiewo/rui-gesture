using RuiGesture.Models.Gesture;

namespace RuiGesture.Models.Config;

using Core;

public class UserConfig
{
    public readonly GestureMachineConfig Core = new();
    public readonly UserInterfaceConfig UI = new();

    public readonly CallbackManager.CallbackContainer Callback;

    public UserConfig(CallbackManager.CallbackContainer callback)
    {
        Callback = callback;
    }
}
﻿namespace RuiGesture.Models.Gesture;

using System;
using System.Linq;
using System.Text;
using System.Threading;
using GetGestureMachineResult = System.Tuple<GestureMachineCluster,
    System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>?,
    System.Exception>;
using Core;
using Config;
using UserScript;

public class ReloadableGestureMachine : IGestureMachine, IDisposable
{
    internal GestureMachineCluster _instance = new NullGestureMachineCluster();

    private GestureMachineCluster Instance
    {
        get => _instance;
        set
        {
            var old = Instance;
            _instance = value;
            old?.Stop();
            old?.Dispose();
        }
    }

    public bool IsActivated()
        => Instance.GetType() != typeof(NullGestureMachineCluster);

    public bool Input(IPhysicalEvent physicalEvent, System.Drawing.Point? point)
        => Instance.Input(physicalEvent, point);

    public bool Input(IPhysicalEvent physicalEvent)
        => Instance.Input(physicalEvent);

    public void Reset()
        => Instance.Reset();

    private readonly GlobalConfig _config;
    private readonly UI.MainFormBase _mainForm;

    public ReloadableGestureMachine(GlobalConfig config, UI.MainFormBase mainForm)
    {
        _config = config;
        _mainForm = mainForm;
    }

    public event EventHandler Reloaded;

    protected virtual void OnReloaded(EventArgs args) => Reloaded?.Invoke(this, args);

    private GetGestureMachineResult GetGestureMachine()
    {
        var restoreFromCache = !IsActivated() || !_config.CLIOption.NoCache;
        var saveCache = !_config.CLIOption.NoCache;
        var userScriptString =
            _config.GetOrSetDefaultUserScriptFile(Encoding.UTF8.GetString(Properties.Resources.DefaultUserScript));

        var candidate = new GestureMachineCandidate(
            _config.UserDirectory,
            userScriptString,
            _config.UserScriptCacheFile,
            restoreFromCache);

        Verbose.Print($"restoreFromCache: {restoreFromCache}");
        Verbose.Print($"saveCache: {saveCache}");
        Verbose.Print($"candidate.IsRestorable: {candidate.IsRestorable}");

        if (candidate.IsRestorable)
        {
            var ctx = new UserScriptExecutionContext(_config, _mainForm);
            try
            {
                return new GetGestureMachineResult(candidate.Restore(ctx), null, null);
            }
            catch (Exception ex)
            {
                Verbose.Error(
                    $"GestureMachine restoration was failed; fallback to normal compilation. {ex.ToString()}");
            }
        }

        if (candidate.Errors.Count() > 0)
        {
            Verbose.Print("Error(s) found in the UserScript on compilation phase.");
            return new GetGestureMachineResult(null, candidate.Errors, null);
        }

        Verbose.Print("No error found in the UserScript on compilation phase.");
        {
            var ctx = new UserScriptExecutionContext(_config, _mainForm);
            try
            {
                if (candidate.UserScriptAssemblyCache.PE.Length == 0)
                {
                    Verbose.Print("User script is empty.");
                    return new GetGestureMachineResult(candidate.Create(ctx), null, null);
                }

                UserScript.EvaluateUserScriptAssembly(ctx, candidate.UserScriptAssemblyCache);

                if (saveCache)
                {
                    try
                    {
                        UserScript.SaveUserScriptAssemblyCache(_config.UserScriptCacheFile,
                            candidate.UserScriptAssemblyCache);
                    }
                    catch (Exception ex)
                    {
                        Verbose.Error($"SaveUserScriptAssemblyCache was failed. {ex.ToString()}");
                    }
                }

                Verbose.Print("No error ocurred in the UserScript on evaluation phase.");
                return new GetGestureMachineResult(candidate.Create(ctx), null, null);
            }
            catch (UserScript.EvaluationAbortedException ex)
            {
                Verbose.Print($"UserScript evaluation was aborted. {ex.InnerException.ToString()}");
                return new GetGestureMachineResult(candidate.Create(ctx), null, ex.InnerException);
            }
            catch (Exception ex)
            {
                Verbose.Error($"Error ocurred in the UserScript on evaluation phase. {ex.ToString()}");
                return new GetGestureMachineResult(candidate.Create(ctx), null, ex);
            }
        }
    }

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _reloadRequest = false;
    private bool _loading = false;

    public void HotReload()
    {
        if (_disposed)
        {
            return;
        }

        if (_loading && !_disposed)
        {
            Verbose.Print("Hot-reload request was queued.");
            _reloadRequest = true;
            return;
        }

        _semaphore.Wait();
        try
        {
            if (_disposed)
            {
                return;
            }

            while (true)
            {
                _loading = true;
                _reloadRequest = false;
                using (Verbose.PrintElapsed("Hot-reload GestureMachine"))
                {
                    try
                    {
                        var (gmCluster, compilationErrors, runtimeError) = GetGestureMachine();

                        if (_reloadRequest)
                        {
                            continue;
                        }

                        _reloadRequest = false;

                        if (gmCluster == null)
                        {
                            _mainForm.ShowErrorBalloon(compilationErrors.GetValueOrDefault());
                        }
                        else
                        {
                            gmCluster.Run();
                            Instance = gmCluster;
                            if (runtimeError == null)
                            {
                                _mainForm.ShowInfoBalloon(gmCluster);
                            }
                            else
                            {
                                _mainForm.ShowWarningBalloon(gmCluster, runtimeError);
                            }

                            _mainForm.UpdateTasktrayMessage(gmCluster.Profiles);
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        Verbose.Print("User script cannot be read; the file may be in use by another process.");
                        Verbose.Print("Waiting 1 sec ...");
                        Thread.Sleep(1000);
                        continue;
                    }
                }

                _loading = false;
                if (!_reloadRequest)
                {
                    OnReloaded(new EventArgs());
                    ReleaseUnusedMemory();
                    break;
                }

                Verbose.Print("Hot reload request exists; Retrying...");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void ReleaseUnusedMemory()
    {
        using (Verbose.PrintElapsed("Release unused memory"))
        {
            var before = GC.GetTotalMemory(false);
            GC.Collect(2);
            var after = GC.GetTotalMemory(false);
            Verbose.Print($"GC.GetTotalMemory: {before} -> {after}");
        }
    }

    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        _disposed = true;
        if (disposing)
        {
            _semaphore.Wait();
            try
            {
                var profiles = Instance.Profiles;
                using (var cde = new CountdownEvent(profiles.Count))
                {
                    foreach (var profile in profiles)
                    {
                        profile.UserConfig.Callback.MachineStop += (_s, _e) => { cde.Signal(); };
                    }

                    Instance = null;
                    cde.Wait(1000);
                }
            }
            finally
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }
        }
    }

    ~ReloadableGestureMachine() => Dispose(false);
}
﻿namespace RuiGesture.Models.Gesture;

using UserScript;

public class GestureMachineCandidate
{
    public readonly string UserDirectory;
    public readonly string UserScriptString;
    public readonly string UserScriptCacheFile;
    public readonly bool RestoreAllowed;

    public GestureMachineCandidate(
        string userDirectory,
        string userScriptString,
        string userScriptCacheFile,
        bool allowRestore
    )
    {
        UserDirectory = userDirectory;
        UserScriptString = userScriptString;
        UserScriptCacheFile = userScriptCacheFile;
        RestoreAllowed = allowRestore;
    }

    private Script _parsedUserScript = null;

    public Script ParsedUserScript
    {
        get
        {
            if (_parsedUserScript == null)
            {
                _parsedUserScript = UserScript.ParseScript(
                    UserScriptString,
                    UserDirectory,
                    UserDirectory);
            }

            return _parsedUserScript;
        }
        private set { _parsedUserScript = value; }
    }

    private System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> _errors;

    public System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> Errors
    {
        get
        {
            if (_errors == null)
            {
                _errors = UserScript.CompileUserScript(ParsedUserScript);
            }

            return _errors;
        }
        private set { _errors = value; }
    }

    private UserScriptAssembly.Cache _userScriptAssemblyCache = null;

    public UserScriptAssembly.Cache UserScriptAssemblyCache
    {
        get
        {
            if (_userScriptAssemblyCache == null)
            {
                _userScriptAssemblyCache =
                    UserScript.GenerateUserScriptAssemblyCache(UserScriptCacheFile, UserScriptString, ParsedUserScript);
            }

            return _userScriptAssemblyCache;
        }
        private set { _userScriptAssemblyCache = value; }
    }

    protected GestureMachineCluster Restore(
        UserScriptExecutionContext ctx,
        UserScriptAssembly.Cache userScriptAssembly)
    {
        UserScript.EvaluateUserScriptAssembly(ctx, userScriptAssembly);
        return new GestureMachineCluster(ctx.Profiles);
    }

    public GestureMachineCluster Create(UserScriptExecutionContext ctx) => new(ctx.Profiles);

    private bool _restorationFailed = false;
    private UserScriptAssembly.Cache _restorationCache = null;

    public UserScriptAssembly.Cache RestorationCache
    {
        get
        {
            if (!_restorationFailed && _restorationCache == null)
            {
                _restorationCache = UserScript.LoadUserScriptAssemblyCache(UserScriptCacheFile, UserScriptString);
                if (_restorationCache == null)
                {
                    _restorationFailed = true;
                }
            }

            return _restorationCache;
        }
        private set { _restorationCache = value; }
    }

    public bool IsRestorable
        => RestorationCache != null;

    public GestureMachineCluster Restore(UserScriptExecutionContext ctx)
        => Restore(ctx, RestorationCache);
}
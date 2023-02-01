﻿namespace RuiGesture.Models.Core;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;

public enum StrokeDirection
{
    Up,
    Down,
    Left,
    Right,
}

public abstract class PointProcessor : IDisposable
{
    protected internal readonly TaskFactory taskFactory;

    private readonly CancellationTokenSource tokenSource;

    private readonly int watchInterval;

    private int lastProcessedTickCount = 0;

    private object _lockObject = new();

    private Point? skippedPoint = null;

    public PointProcessor(TaskFactory taskFactory, int watchInterval)
    {
        this.watchInterval = watchInterval;
        this.taskFactory = taskFactory;
        tokenSource = new CancellationTokenSource();
        StartBackgroundTask();
    }

    private void StartBackgroundTask() =>
        taskFactory.StartNew(() =>
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                var currentTickCount = Environment.TickCount;
                lock (_lockObject)
                {
                    if (skippedPoint.HasValue && MustBeProcessed(currentTickCount))
                    {
                        OnProcess(skippedPoint.Value);
                        skippedPoint = null;
                    }
                }

                Thread.Sleep(1);
            }
        }, tokenSource.Token).ContinueWith(t => { tokenSource.Dispose(); });

    private bool MustBeProcessed(int currentTickCount) =>
        watchInterval <= 0 ||
        currentTickCount < lastProcessedTickCount ||
        lastProcessedTickCount + watchInterval < currentTickCount;

    public virtual void Process(Point point)
    {
        var currentTickCount = Environment.TickCount;
        lock (_lockObject)
        {
            if (MustBeProcessed(currentTickCount))
            {
                lastProcessedTickCount = currentTickCount;
                OnProcess(point);
                skippedPoint = null;
            }
            else
            {
                skippedPoint = point;
            }
        }
    }

    internal abstract void OnProcess(Point point);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            tokenSource.Cancel();
        }
    }

    ~PointProcessor() => Dispose(false);
}
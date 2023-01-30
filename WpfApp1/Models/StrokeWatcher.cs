namespace RuiGesture.Models;

using Gesture;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

public class StrokeWatcher : PointProcessor
{
    internal readonly IStrokeCallbackManager callbacks;
    internal readonly int initialStrokeThreshold;
    internal readonly int strokeDirectionChangeThreshold;
    internal readonly int strokeExtensionThreshold;

    internal readonly List<Stroke> strokes = new();
    internal readonly List<Point> buffer = new();

    internal readonly BlockingCollection<Point> queue = new();

    internal override void OnProcess(Point point)
        => queue.Add(point);

    private readonly object lockObject = new();

    public StrokeWatcher(
        IStrokeCallbackManager callbacks,
        TaskFactory taskFactory,
        int initialStrokeThreshold,
        int strokeDirectionChangeThreshold,
        int strokeExtensionThreshold,
        int watchInterval) : base(taskFactory, watchInterval)
    {
        this.callbacks = callbacks;
        this.initialStrokeThreshold = initialStrokeThreshold;
        this.strokeDirectionChangeThreshold = strokeDirectionChangeThreshold;
        this.strokeExtensionThreshold = strokeExtensionThreshold;
        StartBackgroundTask();
    }

    private void StartBackgroundTask() =>
        taskFactory.StartNew(() =>
        {
            try
            {
                foreach (var point in queue.GetConsumingEnumerable())
                {
                    lock (lockObject)
                    {
                        if (_disposed)
                        {
                            break;
                        }

                        buffer.Add(point);

                        if (buffer.Count < 2)
                        {
                            continue;
                        }

                        if (strokes.Count == 0)
                        {
                            if (Stroke.CanCreate(initialStrokeThreshold, buffer.First(), buffer.Last()))
                            {
                                var stroke = new Stroke(strokeDirectionChangeThreshold, strokeExtensionThreshold,
                                    buffer);
                                strokes.Add(stroke);
                                buffer.Clear();
                            }
                        }
                        else
                        {
                            var stroke = strokes.Last();
                            var strokePointsCount = stroke.Points.Count;

                            var res = stroke.Input(buffer);
                            if (stroke != res)
                            {
                                strokes.Add(res);
                                buffer.Clear();
                            }
                            else if (strokePointsCount != stroke.Points.Count)
                            {
                                buffer.Clear();
                            }
                        }

                        if (StrokeIsEstablished)
                        {
                            callbacks.OnStrokeUpdated(this, GetStorkes());
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        });

    public bool StrokeIsEstablished => strokes.Any();

    public IReadOnlyList<Point> GetBufferedPoints()
    {
        lock (lockObject)
        {
            return buffer.ToList();
        }
    }

    public IReadOnlyList<Stroke> GetStorkes()
    {
        lock (lockObject)
        {
            return strokes.Select(s => s.Freeze()).ToList();
        }
    }

    public StrokeSequence GetStrokeSequence()
    {
        lock (lockObject)
        {
            return new StrokeSequence(strokes.Select(x => x.Direction));
        }
    }

    internal bool _disposed { get; private set; } = false;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (lockObject)
            {
                _disposed = true;
                queue.CompleteAdding();
                base.Dispose(true);
            }
        }
    }

    ~StrokeWatcher() => Dispose(false);
}
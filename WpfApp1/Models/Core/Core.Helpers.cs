namespace RuiGesture.Models.Core;

using System;
using System.Collections.Generic;

public class EventCounter<TEvent>
    where TEvent : Event
{
    public class NaturalNumberCounter<T>
    {
        private readonly Dictionary<T, int> Dictionary = new();

        public int this[T key]
        {
            get { return Dictionary.TryGetValue(key, out var count) ? count : 0; }
            set
            {
                if (value < 0)
                {
                    throw new InvalidOperationException("n >= 0");
                }

                Dictionary[key] = value;
            }
        }

        public void CountDown(T key)
        {
            Dictionary[key] = this[key] - 1;
        }

        public void CountUp(T key)
        {
            Dictionary[key] = this[key] + 1;
        }
    }

    private readonly NaturalNumberCounter<TEvent> InvalidReleaseEvents = new();

    public int this[TEvent key]
    {
        get => InvalidReleaseEvents[key];
    }

    public void IgnoreNext(TEvent releaseEvent) => InvalidReleaseEvents.CountUp(releaseEvent);

    public void IgnoreNext(IEnumerable<TEvent> releaseEvents)
    {
        foreach (var releaseEvent in releaseEvents)
        {
            IgnoreNext(releaseEvent);
        }
    }

    public void CountDown(TEvent key) => InvalidReleaseEvents.CountDown(key);
}
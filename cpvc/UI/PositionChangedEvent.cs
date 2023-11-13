using System;
using System.Collections.Generic;
using System.Linq;

namespace CPvC
{
    public enum NotifyListChangedAction
    {
        Added,
        Moved,
        Replaced,
        Removed,
        Cleared
    }

    public class PositionChangedEventArgs<T> : EventArgs
    {
        public PositionChangedEventArgs(List<InterestingEvent> horizontalInterestingEvents)
        {
            HorizontalInterestingEvents = horizontalInterestingEvents;
        }

        public List<InterestingEvent> HorizontalInterestingEvents { get; }
    }

    public delegate void NotifyPositionChangedEventHandler<T>(object sender, PositionChangedEventArgs<T> e);
}

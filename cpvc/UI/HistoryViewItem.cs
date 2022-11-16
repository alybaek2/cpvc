using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CPvC
{
    public class HistoryEventOrderings
    {
        public HistoryEventOrderings(History history)
        {
            Init(history);
        }

        public IEnumerable<HistoryEvent> GetVerticallySorted()
        {
            return _verticalEvents;
        }

        public IEnumerable<HistoryEvent> GetHorizonallySorted()
        {
            return _horizontalEvents;
        }

        public int GetVerticalPosition(HistoryEvent historyEvent)
        {
            return _verticalPosition[historyEvent];
        }

        public int GetHorizontalPosition(HistoryEvent historyEvent)
        {
            return _horizontalPosition[historyEvent];
        }

        public HistoryEvent GetParent(HistoryEvent historyEvent)
        {
            return _parentEvents[historyEvent];
        }

        public int Count()
        {
            return _verticalEvents.Count;
        }

        public bool VerticalPositionChanged(HistoryEvent historyEvent)
        {
            // Has "interestingness" changed?
            if (InterestingEvent(historyEvent))
            {
                if (!_verticalEvents.Contains(historyEvent))
                {
                    return true;
                }
            }
            else
            {
                return _verticalEvents.Contains(historyEvent);
            }

            int v = GetVerticalPosition(historyEvent);
            if (v < _verticalEvents.Count - 1)
            {
                if (VerticalSort(historyEvent, _verticalEvents[v + 1]) >= 0)
                {
                    return true;
                }
            }

            if (v > 0)
            {
                if (VerticalSort(_verticalEvents[v - 1], historyEvent) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        static private bool InterestingEvent(HistoryEvent historyEvent)
        {
            if (historyEvent is RootHistoryEvent ||
                historyEvent is BookmarkHistoryEvent ||
                historyEvent.Children.Count != 1)
            {
                return true;
            }

            return false;
        }

        private int HorizontalSort(HistoryEvent x, HistoryEvent y)
        {
            int result = y.GetMaxDescendentTicks().CompareTo(x.GetMaxDescendentTicks());

            if (result != 0)
            {
                return result;
            }

            return y.Id.CompareTo(x.Id);
        }

        private int VerticalSort(HistoryEvent x, HistoryEvent y)
        {
            if (x.Ticks < y.Ticks)
            {
                return -1;
            }
            else if (x.Ticks > y.Ticks)
            {
                return 1;
            }
            else
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }
                else if (x.IsEqualToOrAncestorOf(y))
                {
                    return -1;
                }
                else if (y.IsEqualToOrAncestorOf(x))
                {
                    return 1;
                }
            }

            return x.Id.CompareTo(y.Id);
        }

        private void Init(History history)
        {
            _parentEvents = new Dictionary<HistoryEvent, HistoryEvent>();
            _horizontalEvents = new List<HistoryEvent>();

            _verticalEvents = new List<HistoryEvent>();
            List<HistoryEvent> children = new List<HistoryEvent>();

            _horizontalEvents.Add(history.RootEvent);

            int i = 0;

            while (i < _horizontalEvents.Count)
            {
                children.Clear();
                children.AddRange(_horizontalEvents[i].Children);
                children.Sort((x, y) => HorizontalSort(x, y));

                if (!InterestingEvent(_horizontalEvents[i]))
                {
                    _horizontalEvents.RemoveAt(i);
                    i--;
                }
                else
                {
                    _verticalEvents.Add(_horizontalEvents[i]);
                }

                _horizontalEvents.InsertRange(i + 1, children);
                i++;
            }

            // Figure out parents.
            foreach (HistoryEvent historyEvent in _horizontalEvents)
            {
                HistoryEvent parentEvent = historyEvent.Parent;
                while (parentEvent != null && !_verticalEvents.Contains(parentEvent))
                {
                    parentEvent = parentEvent.Parent;
                }

                _parentEvents.Add(historyEvent, parentEvent);
            }

            _verticalEvents.Sort((x, y) => VerticalSort(x, y));

            _horizontalPosition = new Dictionary<HistoryEvent, int>();
            _verticalPosition = new Dictionary<HistoryEvent, int>();

            for (int v = 0; v < _verticalEvents.Count; v++)
            {
                _verticalPosition.Add(_verticalEvents[v], v);
            }

            for (int h = 0; h < _horizontalEvents.Count; h++)
            {
                _horizontalPosition.Add(_horizontalEvents[h], h);
            }
        }

        private Dictionary<HistoryEvent, HistoryEvent> _parentEvents;
        private List<HistoryEvent> _horizontalEvents;
        private List<HistoryEvent> _verticalEvents;
        private Dictionary<HistoryEvent, int> _horizontalPosition;
        private Dictionary<HistoryEvent, int> _verticalPosition;
    }

    public class HistoryViewNode
    {
        public HistoryViewNode(HistoryEvent historyEvent)
        {
            HistoryEvent = historyEvent;
            Parent = null;
            Children = new List<HistoryViewNode>();
        }

        public HistoryEvent HistoryEvent { get; }
        public HistoryViewNode Parent { get; set; }
        public List<HistoryViewNode> Children { get; }
    }

    /// <summary>
    /// Represents a single HistoryEvent in the history view used in the BookmarkSelectWdinow
    /// and MachinePropertiesWindow classes. These events can be either a bookmark, the root
    /// node of the timeline, or the terminus of a branch.
    /// </summary>
    public class HistoryViewItem
    {
        // The event represented by this HistoryViewItem
        public HistoryEvent HistoryEvent { get; }

        // The full set of events shown in this item. This will include "HistoryEvent", and the rest are "passthrough"
        // of other history events. In the history view "passthrough" is shown as a vertical line.
        public List<HistoryEvent> Events { get; set; }

        public Canvas Canvas { get; private set; }

        private const double _scalingX = 16;
        private const double _scalingY = 16;

        public HistoryViewItem(HistoryEvent historyEvent)
        {
            HistoryEvent = historyEvent ?? throw new ArgumentNullException(nameof(historyEvent));

            Canvas = null;
            Canvas = new Canvas();
            Events = new List<HistoryEvent>();
        }

        private int EventIndex(HistoryEvent historyEvent)
        {
            return Events.IndexOf(historyEvent);
        }

        private int EventAncestorIndex(HistoryEvent historyEvent)
        {
            return Events.FindIndex(descendant => historyEvent?.IsEqualToOrAncestorOf(descendant) ?? false);
        }

        public int AddEvent(int minIndex, HistoryEvent historyEvent)
        {
            int padding = minIndex - Events.Count;
            for (int i = 0; i < padding; i++)
            {
                Events.Add(null);
            }

            Events.Add(historyEvent);

            return Events.Count - 1;
        }

        private void DrawDot(double x, Brush brush, bool filled)
        {
            double radius = 0.25;
            double top = 0.5 - radius;

            Ellipse circle = new Ellipse
            {
                Stroke = brush,
                Fill = filled ? brush : Brushes.White,
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                UseLayoutRounding = true,
                Margin = new Thickness(_scalingX * (x - radius), _scalingY * top, 0, 0),
                Width = _scalingX * 2 * radius,
                Height = _scalingY * 2 * radius
            };

            Canvas.Children.Add(circle);

            // Ensure the dot is always "on top".
            Canvas.SetZIndex(circle, 100);
        }

        private void DrawLine(double x0, double y0, double x1, double y1, Brush brush)
        {
            Line line = new Line
            {
                X1 = _scalingX * x0,
                Y1 = _scalingY * y0,
                X2 = _scalingX * x1,
                Y2 = _scalingY * y1,
                StrokeThickness = 2,
                Stroke = brush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                UseLayoutRounding = true
            };

            Canvas.Children.Add(line);

            // Ensure lines are never "on top" of dots.
            Canvas.SetZIndex(line, 1);
        }

        /// <summary>
        /// Draws the HistoryViewItem, rendering it to the Canvas.
        /// </summary>
        /// <param name="next">The next chronological HistoryViewItem.</param>
        /// <param name="currentEvent">The current event in the machine's history.</param>
        public void Draw(HistoryViewItem next, HistoryEvent currentEvent)
        {
            for (int t = 0; t < Events.Count; t++)
            {
                Draw(Events[t], t, next, currentEvent);
            }
        }

        /// <summary>
        /// Draws a given history event at the given position.
        /// </summary>
        /// <param name="historyEvent">The history event to draw.</param>
        /// <param name="x">The position at which to draw the event.</param>
        /// <param name="next">The next chronological HistoryViewItem.</param>
        /// <param name="currentEvent">The current event in the machine's history.</param>
        private void Draw(HistoryEvent historyEvent, int x, HistoryViewItem next, HistoryEvent currentEvent)
        {
            if (historyEvent == null)
            {
                return;
            }

            //Canvas = new Canvas();

            // Calculate the "centre" of the cell at position x.
            double cx = x + 0.5;
            double cy = 0.5;

            // If the history event isn't "ours", then draw it as a "passthrough" line.
            bool passthrough = (HistoryEvent != historyEvent);

            if (historyEvent.Parent != null)
            {
                // Draw a line from our parent.
                DrawLine(cx, 1, cx, cy, Brushes.DarkBlue);
            }

            BookmarkHistoryEvent bookmarkHistoryEvent = historyEvent as BookmarkHistoryEvent;
            if (bookmarkHistoryEvent != null && bookmarkHistoryEvent.Bookmark != null && !passthrough)
            {
                // User bookmarks are drawn as a red dot, system bookmarks as dark red.
                DrawDot(cx, bookmarkHistoryEvent.Bookmark.System ? Brushes.DarkRed : Brushes.Crimson, historyEvent != currentEvent);
            }

            if (passthrough)
            {
                // A "passthrough" event is drawn as a straight line.
                int childNextX = next.EventIndex(historyEvent);
                if (childNextX != -1)
                {
                    DrawLine(cx, cy, childNextX + 0.5, 0, Brushes.DarkBlue);
                }
            }
            else if (next != null && historyEvent.Children.Count >= 1)
            {
                // For history events with children, draw a line from the centre of the cell to each child
                for (int c = 0; c < historyEvent.Children.Count; c++)
                {
                    int childNextX = next.EventAncestorIndex(historyEvent.Children[c]);
                    if (childNextX != -1)
                    {
                        DrawLine(cx, cy, childNextX + 0.5, 0, Brushes.DarkBlue);
                    }
                }
            }
            else
            {
                // History events with no children are drawn as a terminating dot.
                DrawDot(cx, (bookmarkHistoryEvent != null) ? (bookmarkHistoryEvent.Bookmark.System ? Brushes.DarkRed : Brushes.Crimson) : Brushes.DarkBlue, historyEvent != currentEvent);
            }
        }
    }
}

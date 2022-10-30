using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CPvC
{
    internal class HistoryEventVerticalComparer : IComparer<HistoryEvent>
    {
        public int Compare(HistoryEvent x, HistoryEvent y)
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

            return 0;
        }
    }

    public class HistoryViewNodeList
    {
        public HistoryViewNodeList()
        {
            _nodeList = new List<HistoryViewNode>();

            _verticalOrdering = new List<HistoryEvent>();
            _horizontalOrdering = new List<HistoryEvent>();
            _verticalOrdering2 = new SortedList<HistoryEvent, HistoryViewNode>(new HistoryEventVerticalComparer());
        }

        public IList<HistoryViewNode> NodeList
        {
            get
            {
                return _verticalOrdering2.Values;
            }
        }

        public void Add(HistoryEvent historyEvent)
        {
            // Check the "interestingness" of the parent. Either it will now be interesting (where before it wasn't), or it was
            // interesting and no longer is.
            HistoryEvent parentEvent = historyEvent.Parent;
            bool parentInteresting = false;
            bool present = false;
            if (parentEvent != null)
            {
                parentInteresting = InterestingEvent(parentEvent);
                present = _verticalOrdering2.ContainsKey(parentEvent);
            }
            if (present && !parentInteresting)
            {
                _verticalOrdering2.Remove(parentEvent);
            }
            else if (!present && parentInteresting)
            {
                _verticalOrdering2.Add(parentEvent, null);
            }

            // Add this event and all its children, if they're interesting!
            Queue<HistoryEvent> historyEvents = new Queue<HistoryEvent>();
            historyEvents.Enqueue(historyEvent);

            while (historyEvents.Any())
            {
                HistoryEvent he = historyEvents.Dequeue();
                foreach (HistoryEvent child in he.Children)
                {
                    historyEvents.Enqueue(child);
                }


                if (!InterestingEvent(he))
                {
                    continue;
                }

                HistoryEvent interestingParentEvent = GetParentNodeOrCreateIt(he);

                //if (_verticalOrdering2.ContainsKey(he))
                //{
                //    throw new ArgumentException();
                //}

                //// Find parent!
                //// GAH! Parent might not be shown if it only has one child!
                //HistoryViewNode parentNode = null;
                //HistoryEvent parent = he.Parent;
                //bool addParent = parent != null && !_verticalOrdering2.ContainsKey(parent) && InterestingEvent(parent);

                //HistoryEvent last = parent;
                //while (parent != null)
                //{
                //    if (parent is RootHistoryEvent)
                //    {
                //        string breakymcbreakpoint = "";
                //    }
                //    if (_verticalOrdering2.ContainsKey(parent))
                //    {
                //        parentNode = _verticalOrdering2[parent];
                //        break;
                //    }

                //    last = parent;
                //    parent = parent.Parent;
                //}

                //if (addParent)
                //{
                //    foreach (HistoryViewNode childNode in parentNode.Children)
                //    {
                //        if (parentNode.HistoryEvent.IsEqualToOrAncestorOf(childNode.HistoryEvent))
                //        {
                //            // This is the one we need to move!
                //            HistoryViewNode newParent = new HistoryViewNode(he.Parent);
                //            parentNode.Children.Remove(childNode);
                //            parentNode.Children.Add(newParent);
                //            newParent.Parent = parentNode;
                //            childNode.Parent = newParent;
                //            newParent.Children.Add(childNode);
                //            _verticalOrdering2.Add(he.Parent, newParent);

                //            parentNode = newParent;

                //            break;
                //        }
                //    }



                //}

                //// Re-evaluate whether the parent is still interesting!
                //else if (parentNode != null && !InterestingEvent(parentNode.HistoryEvent))
                //{
                //    HistoryViewNode newParentNode = parentNode.Parent;
                //    newParentNode.Children.AddRange(parentNode.Children);
                //    newParentNode.Children.Remove(parentNode);
                //    parentNode.Parent = null;
                //    parentNode.Children.Clear();
                //    _verticalOrdering2.Remove(parentNode.HistoryEvent);

                //    parentNode = newParentNode;
                //}

                //HistoryViewNode node = new HistoryViewNode(historyEvent);
                //node.Parent = parentNode;
                //if (parentNode != null)
                //{
                //    parentNode.Children.Add(node);
                //}

                //_verticalOrdering2.Add(historyEvent, node);
                //bool c = _verticalOrdering2.ContainsKey(historyEvent);

                // Horizontal ordering! Can this be deferred until rendering? Just do one horizontal sort at render time!
                // Maybe track horizontal ordering later as an optimization!
            }
        }

        private HistoryEvent GetParentNodeOrCreateIt(HistoryEvent historyEvent)
        {
            HistoryEvent parentEvent = historyEvent.Parent;
            while (parentEvent != null)
            {
                if (InterestingEvent(parentEvent))
                {
                    if (_verticalOrdering2.ContainsKey(parentEvent))
                    {
                        // Found it!
                        return parentEvent;
                    }
                    else
                    {
                        // Create it!

                        _verticalOrdering2.Add(parentEvent, null);
                        return parentEvent;
                    }
                }
                //else
                //{
                //    if (_verticalOrdering2.ContainsKey(parentEvent))
                //    {
                //        _verticalOrdering2.Remove(parentEvent);
                //    }
                //}

                parentEvent = parentEvent.Parent;
            }

            return null;
            //throw new Exception("There should be a parent!");
        }

        public void Update(HistoryEvent historyEvent)
        {
            HistoryViewNode node = _verticalOrdering2[historyEvent];

            // Might be able to optimize this by checking the events on either side of this one... did the ordering change?
            _verticalOrdering2.Remove(historyEvent);
            _verticalOrdering2.Add(historyEvent, node);
        }

        public void Delete(HistoryEvent historyEvent, bool recursive)
        {
            HistoryViewNode node = _verticalOrdering2[historyEvent];

            if (!recursive)
            {
                // Need to add check here for if parent is still interesting? Maybe not necessary?
                HistoryViewNode parentNode = node.Parent;
                parentNode.Children.AddRange(node.Children);
                parentNode.Children.Remove(node);
                node.Parent = null;
                node.Children.Clear();
                _verticalOrdering2.Remove(node.HistoryEvent);
            }
            else
            {
                Queue<HistoryViewNode> nodes = new Queue<HistoryViewNode>();
                nodes.Enqueue(node);

                while (nodes.Any())
                {
                    HistoryViewNode d = nodes.Dequeue();

                    d.Parent.Children.Remove(d);
                    d.Parent = null;

                    _verticalOrdering2.Remove(d.HistoryEvent);

                    foreach (HistoryViewNode c in d.Children)
                    {
                        nodes.Enqueue(c);
                    }

                    d.Children.Clear();
                }

                // Need to add check if the deleted node's parent is now interesting?
            }

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

        private List<HistoryViewNode> _nodeList;

        private List<HistoryEvent> _verticalOrdering;
        private List<HistoryEvent> _horizontalOrdering;
        private SortedList<HistoryEvent, HistoryViewNode> _verticalOrdering2;
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

        public void Draw()
        {

        }
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

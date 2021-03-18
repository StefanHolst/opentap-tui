using OpenTap;
using OpenTap.Diagnostic;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace OpenTap.Tui.Views
{
    class LogEvent
    {
        public string Message { get; set; }
        public int EventType { get; set; }

        public LogEvent(Event e)
        {
            Message = e.Message;
            EventType = e.EventType;
        }
        
        public override string ToString()
        {
            return Message;
        }
    }
    
    public class LogPanelView : ListView, ILogListener
    {
        private static List<LogEvent> messages = new List<LogEvent>();
        private static object lockObj = new object();
        private static bool listenerAdded = false;
        private static Action RefreshAction;
        private View parent;

        static LogPanelView()
        {
            lock (lockObj)
            {
                if (listenerAdded == false)
                {
                    listenerAdded = true;
                    Log.AddListener(new LogPanelView());
                }
            }
        }
        
        public LogPanelView(View parent = null)
        {
            this.parent = parent;
            RefreshAction += Refresh;
            
            CanFocus = true;
            SetSource(messages);
        }

        private void Refresh()
        {
            lock (lockObj)
            {
                if (parent == null || parent == Application.Current)
                    Application.MainLoop?.Invoke(() => Update(true));
                else
                    Update(false);
            }
        }

        private void Update(bool setNeedsDisplay)
        {
            lock (messages)
            {
                if (messages.Any())
                {
                    if (Bounds.Height > 0)
                        top = Math.Max(0, messages.Count - Bounds.Height);
                    SelectedItem = messages.Count - 1;
                    if (setNeedsDisplay)
                        SetNeedsDisplay();
                }
            }
        }

        public void EventsLogged(IEnumerable<Event> Events)
        {
            lock (messages)
            {
                messages.AddRange(Events.Select(e => new LogEvent(e)));
            }

            RefreshAction?.Invoke();
        }

        public void Flush()
        {
            RefreshAction?.Invoke();
        }
        
        public override void Redraw (Rect bounds)
        {
            var current = ColorScheme.Focus;
            Driver.SetAttribute (current);
            Move (0, 0);
            var f = Frame;
            if (selected < top) {
                top = selected;
            } else if (selected >= top + f.Height) {
                top = selected;
            }
            var item = top;
            bool focused = HasFocus;
            int col = AllowsMarking ? 4 : 0;

            for (int row = 0; row < f.Height; row++, item++) {
                bool isSelected = item == selected;

                var newcolor = focused ? (isSelected ? ColorScheme.Focus : ColorScheme.Normal) : ColorScheme.Normal;
                if (item < messages.Count && TuiSettings.Current.UseLogColors)
                {
                    var message = messages[item];
                    switch (message.EventType)
                    {
                        case (int)LogEventType.Debug:
                            newcolor = Attribute.Make(newcolor.Background == Color.DarkGray ? Color.Gray : Color.DarkGray, newcolor.Background);
                            break;
                        case (int)LogEventType.Information:
                            newcolor = Attribute.Make(newcolor.Background == Color.White ? Color.Gray : Color.White, newcolor.Background);
                            break;
                        case (int)LogEventType.Warning:
                            newcolor = Attribute.Make(newcolor.Background == Color.BrightYellow ? Color.Brown : Color.BrightYellow, newcolor.Background);
                            break;
                        case (int)LogEventType.Error:
                            newcolor = Attribute.Make(newcolor.Background == Color.BrightRed ? Color.Red : Color.BrightRed, newcolor.Background);
                            break;
                    }
                }
                
                Driver.SetAttribute (newcolor);

                Move (0, row);
                if (Source == null || item >= Source.Count) {
                    for (int c = 0; c < f.Width; c++)
                        Driver.AddRune (' ');
                } else {
                    if (AllowsMarking) {
                        Driver.AddStr (Source.IsMarked (item) ? (AllowsMultipleSelection ? "[x] " : "(o)") : (AllowsMultipleSelection ? "[ ] " : "( )"));
                    }
                    Source.Render (this, Driver, isSelected, item, col, row, f.Width - col);
                }
            }
        }
    }
}

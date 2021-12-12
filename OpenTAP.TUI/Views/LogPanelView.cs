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

        private ColorScheme backgroundScheme = new ColorScheme();
        private ColorScheme debugScheme = new ColorScheme();
        private ColorScheme infoScheme = new ColorScheme();
        private ColorScheme warningScheme = new ColorScheme();
        private ColorScheme errorScheme = new ColorScheme();

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
            UpdateColorTheme();
            TuiSettings.Current.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Theme))
                    UpdateColorTheme();
            };
            
            this.parent = parent;
            RefreshAction += Refresh;
            
            CanFocus = true;
            SetSource(messages);
        }

        private void UpdateColorTheme()
        {
            var currentColor = TuiSettings.Current.BaseColor;
            backgroundScheme.Normal = Application.Driver.MakeAttribute(currentColor.NormalForeground, currentColor.NormalBackground);
            
            debugScheme.Normal = Application.Driver.MakeAttribute(currentColor.NormalBackground == Color.DarkGray ? Color.Gray : Color.DarkGray, currentColor.NormalBackground);
            debugScheme.Focus = Application.Driver.MakeAttribute(currentColor.FocusBackground == Color.DarkGray ? Color.Gray : Color.DarkGray, currentColor.FocusBackground);

            infoScheme.Normal = Application.Driver.MakeAttribute(currentColor.NormalBackground == Color.White ? Color.Gray : Color.White, currentColor.NormalBackground);
            infoScheme.Focus = Application.Driver.MakeAttribute(currentColor.FocusBackground == Color.White || currentColor.FocusBackground == Color.Gray ? Color.DarkGray : Color.White, currentColor.FocusBackground);

            warningScheme.Normal = Application.Driver.MakeAttribute(currentColor.NormalBackground == Color.BrightYellow ? Color.Brown : Color.BrightYellow, currentColor.NormalBackground);
            warningScheme.Focus = Application.Driver.MakeAttribute(currentColor.FocusBackground == Color.BrightYellow ? Color.Brown : Color.BrightYellow, currentColor.FocusBackground);

            errorScheme.Normal = Application.Driver.MakeAttribute(currentColor.NormalBackground == Color.BrightRed ? Color.Red : Color.BrightRed, currentColor.NormalBackground);
            errorScheme.Focus = Application.Driver.MakeAttribute(currentColor.FocusBackground == Color.BrightRed ? Color.Red : Color.BrightRed, currentColor.FocusBackground);
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
                        TopItem = Math.Max(0, messages.Count - Bounds.Height);
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
            int col = 2;

            for (int row = 0; row < f.Height; row++, item++) {
                bool isSelected = item == selected;

                if (item < messages.Count && TuiSettings.Current.UseLogColors)
                {
                    var message = messages[item];
                    switch (message.EventType)
                    {
                        case (int)LogEventType.Debug:
                            Driver.SetAttribute(focused && isSelected ? debugScheme.Focus : debugScheme.Normal);
                            break;
                        case (int)LogEventType.Information:
                            Driver.SetAttribute(focused ? (isSelected ? infoScheme.Focus : infoScheme.Normal) : infoScheme.Normal);
                            break;
                        case (int)LogEventType.Warning:
                            Driver.SetAttribute(focused ? (isSelected ? warningScheme.Focus : warningScheme.Normal) : warningScheme.Normal);
                            break;
                        case (int)LogEventType.Error:
                            Driver.SetAttribute(focused ? (isSelected ? errorScheme.Focus : errorScheme.Normal) : errorScheme.Normal);
                            break;
                    }
                }
                else
                {
                    Driver.SetAttribute(backgroundScheme.Normal);
                }
                
                Move (0, row);
                if (Source == null || item >= Source.Count) {
                    for (int c = 0; c < f.Width; c++)
                        Driver.AddRune (' ');
                } else {
                    if (isSelected)
                        Driver.AddStr("> ");
                    Source.Render (this, Driver, isSelected, item, col, row, f.Width - col);
                }
            }
        }
    }
}

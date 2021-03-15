using OpenTap;
using OpenTap.Diagnostic;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class LogPanelView : ListView, ILogListener
    {
        private static List<string> messages = new List<string>();
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
                messages.AddRange(Events.Select(e => e.Message));
            }

            RefreshAction?.Invoke();
        }

        public void Flush()
        {
            RefreshAction?.Invoke();
        }
    }
}

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
        private static Action refreshAction;
        private View parent;

        public LogPanelView(View parent)
        {
            this.parent = parent;
            lock (lockObj)
            {
                if (listenerAdded == false)
                {
                    listenerAdded = true;
                    Log.AddListener(this);
                }

                refreshAction += Refresh;
                parent.LayoutComplete += (_) =>  Refresh();
            }
            
            CanFocus = true;
            SetSource(messages);
        }

        private void Refresh()
        {
            lock (lockObj)
            {
                if (Application.Current == parent)
                    Application.MainLoop.Invoke(Update);
            }
        }

        private void Update()
        {
            lock (messages)
            {
                if (messages.Any()) 
                {
                    TopItem = Math.Max(0, messages.Count - Bounds.Height);
                    SelectedItem = messages.Count - 1;
                }
            }
        }

        public void EventsLogged(IEnumerable<Event> Events)
        {
            lock (messages)
            {
                messages.AddRange(Events.Select(e => e.Message));
            }

            refreshAction?.Invoke();
        }

        public void Flush()
        {
            refreshAction?.Invoke();
        }
    }
}

using OpenTap;
using OpenTap.Diagnostic;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class LogPanelView : ListView, ILogListener
    {
        private static List<string> messages = new List<string>();
        private static object lockObj = new object();

        public LogPanelView()
        {
            Log.AddListener(this);
            CanFocus = true;
        }

        private void Refresh()
        {
            lock (lockObj)
            {
                Application.MainLoop.Invoke(Update);
            }
        }

        private void Update()
        {
            lock (messages)
            {
                if (messages.Any()) 
                {
                    SetSource(messages);
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
            Refresh();
        }

        public void Flush()
        {
            Refresh();
        }
    }
}

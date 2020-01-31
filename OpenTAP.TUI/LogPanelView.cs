﻿using OpenTap;
using OpenTap.Diagnostic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class LogPanelView : ListView, ILogListener
    {
        private static List<string> messages = new List<string>();

        public LogPanelView()
        {
            Log.AddListener(this);
            CanFocus = true;

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ListView.Frame))
                {
                    Update();
                }
            };
        }

        private void Update()
        {
            SetSource(messages);

            if (messages.Any()) 
            {
                TopItem = Math.Max(0, messages.Count - Bounds.Height);
                SelectedItem = messages.Count - 1;
                Application.Refresh();
            }
        }

        public void EventsLogged(IEnumerable<Event> Events)
        {
            messages.AddRange(Events.Where(e => e.EventType < 40).Select(e => e.Message));
            Update();
        }

        public void Flush()
        {
            SetSource(messages);
        }
    }
}

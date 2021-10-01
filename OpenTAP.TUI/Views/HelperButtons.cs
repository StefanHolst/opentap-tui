using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class HelperButtons : View
    {
        public static HelperButtons Instance = null;
        private static List<MenuItem> actions = null;
        
        public HelperButtons()
        {
            Instance = this;
        }

        public static void SetActions(List<MenuItem> actions)
        {
            if (Instance == null)
                return;
            Instance.RemoveAll();
            
            int offset = 0;
            for (int i = 0; i < actions.Count; i++)
            {
                var item = actions[i];
                if (item.IsEnabled() == false)
                    continue;
                
                var title = $"F{i + 5} {item.Title}";
                var b = new Button(title)
                {
                    X = offset,
                    HotKeySpecifier = 0xFFFF // Disable hotkey
                };
                b.Clicked += item.Action;
                
                Instance.Add(b);
                offset += title.Length + 4;
            }

            HelperButtons.actions = actions;
            Instance.SetNeedsDisplay();
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            var keyValue = keyEvent.KeyValue - (int) Key.F5;
            if (keyValue <= 4 && keyValue >= 0 && actions.Count > keyValue)
            {
                var action = actions[keyValue];
                if (action.IsEnabled())
                    action.Action.Invoke();
            }
            
            return base.ProcessKey(keyEvent);
        }
    }
}
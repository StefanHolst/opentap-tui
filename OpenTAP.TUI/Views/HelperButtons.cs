using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class HelperButtons : View
    {
        private static View Owner = null;
        private List<MenuItem> actions = null;
        
        public void SetActions(List<MenuItem> actions, View owner)
        {
            Owner = owner;
            RemoveAll();
            
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
                
                Add(b);
                offset += title.Length + 4;
            }

            this.actions = actions;
        }

        public override bool ProcessHotKey(KeyEvent keyEvent)
        {
            var keyValue = keyEvent.KeyValue - (int) Key.F5;
            if (keyValue <= 4 && keyValue >= 0 && actions.Count > keyValue)
            {
                var action = actions[keyValue];
                if (action.IsEnabled())
                    action.Action.Invoke();
                return true;
            }
            
            return base.ProcessHotKey(keyEvent);
        }
    }
}
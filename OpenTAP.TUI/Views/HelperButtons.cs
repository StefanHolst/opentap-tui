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
            Clear();
            
            int offset = 0;
            for (int i = 0; i < actions.Count; i++)
            {
                var item = actions[i];
                var title = $"{item.Shortcut} {item.Title}";
                var b = new Button(title)
                {
                    X = offset,
                    HotKeySpecifier = 0xFFFF // Disable hotkey
                };
                b.Clicked += item.Action;
                b.Visible = item.IsEnabled();
                
                Add(b);
                offset += title.Length + 4;
            }

            this.actions = actions;
        }

        public override bool ProcessHotKey(KeyEvent keyEvent)
        {
            foreach (var item in actions)
            {
                if (keyEvent.Key == item.Shortcut)
                {
                    if (item.IsEnabled())
                    {
                        item.Action.Invoke();
                    }
                    return true;
                }
            }

            return base.ProcessHotKey(keyEvent);
        }
    }
}
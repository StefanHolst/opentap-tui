using System;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class TextViewWithEnter : TextView
    {
        public bool CloseOnEnter { get; set; } = true;

        public Action Closing;

        public override bool ProcessKey(KeyEvent kb)
        {
            if (kb.Key == Key.Enter && CloseOnEnter)
            {
                Closing();
                return false;
            }

            if (kb.Key == Key.Esc && !CloseOnEnter)
            {
                // invoke a new Enter command while accepting to closing on it.
                CloseOnEnter = true;
                Application.Current.ProcessKey(new KeyEvent() {Key = Key.Enter});
                return true;
            }
            return base.ProcessKey(kb);
        }
    }
}
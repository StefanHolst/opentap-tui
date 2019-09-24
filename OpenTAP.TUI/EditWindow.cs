using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class EditWindow : Window
    {
        public EditWindow(string title) : base(title)
        {

        }

        private void closing(View view, bool edited)
        {
            foreach (var item in view.Subviews)
            {
                closing(item, edited);
            }
            view.InvokeClosing(this, edited);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            var handled = base.ProcessKey(keyEvent);

            if (keyEvent.Key == Key.Esc)
            {
                closing(this, false);
                Application.RequestStop();
                return true;
            }
            if (keyEvent.Key == Key.Enter)
            {
                closing(this, true);
                Application.RequestStop();
                return true;
            }

            return handled;
        }
    }
}

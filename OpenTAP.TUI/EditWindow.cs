using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class EditWindow : Window
    {
        public event EventHandler<bool> Closing;
        
        public EditWindow(string title) : base(title)
        {
        }

        private void closing(View view, bool edited)
        {
            foreach (var item in view.Subviews)
            {
                closing(item, edited);
            }
            Closing?.Invoke(this, edited);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
            {
                var handled = base.ProcessKey(keyEvent);
                if (handled) return true;
                closing(this, false);
                Application.RequestStop();
                return true;
            }
            if (keyEvent.Key == Key.Enter)
            {
                var handled = base.ProcessKey(keyEvent);
                if (handled == false)
                {
                    closing(this, true);
                    Application.RequestStop();
                    return true;
                }
                return false;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}

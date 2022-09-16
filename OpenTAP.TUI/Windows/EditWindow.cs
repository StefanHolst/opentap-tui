using System.Diagnostics;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class EditWindow : BaseWindow
    {
        public EditWindow(string title) : base(title)
        {
            Modal = true;
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
            {
                var handled = base.ProcessKey(keyEvent);
                if (handled) return true;
                Application.RequestStop();
                return true;
            }
            if (keyEvent.Key == Key.Enter)
            {
                var handled = base.ProcessKey(keyEvent);
                if (handled == false)
                {
                    Application.RequestStop();
                    return true;
                }
                return handled;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}

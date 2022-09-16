using NStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public abstract class BaseWindow : Window
    {
        protected BaseWindow(ustring title = null) : base(title)
        {
        }

        public override bool ProcessHotKey(KeyEvent keyEvent)
        {
            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Kill))
            {
                TUI.Log.Info("Tui process killed.");
                Process.GetCurrentProcess().Kill();
                return false;
            }
            return base.ProcessHotKey(keyEvent);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class HelpWindow : EditWindow
    {
        public HelpWindow() : base("Help")
        {
            var text = new TextView()
            {
                Text =
@"Navigate using arrows, 'TAB' and 'Enter'. Open the menu using 'F9'.

Move steps using space to select a step, then navigate to the place you want to drop the step and press space.
You can also use right arrow ('>') to insert a step into another step as a child step.",
                ReadOnly = true
            };
            Add(text);

        }
    }
}

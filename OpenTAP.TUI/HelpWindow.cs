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
@"Use 'F9' to open the top menu, or use 'alt' + ('f'|'e'|'s'|'r'|'h').
Use 'tab' to switch selected panel, or use 'F1'-'F4' to switch to specific panel.
Move steps using space to select a step, then navigate to the place you want to drop the step and press space.
You can also use right arrow ('>') to insert a step into another step as a child step.
Use 'ctrl' + 's' in the testplan to save.
Use 'ctrl' + 'x' or 'ctrl' + 'c' to quit.",
                ReadOnly = true
            };
            Add(text);

        }
    }
}

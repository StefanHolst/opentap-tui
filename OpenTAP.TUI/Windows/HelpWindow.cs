using OpenTap.Tui.Views;
using System.Collections.Generic;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class HelpWindow : EditWindow
    {
        public HelpWindow() : base("Help")
        {
            Modal = true;

            var text = new TextView()
            {
                Text =
$@"Highlight the tool bar using F9.
View and change shortcut by navigating to settings -> TUI Settings -> Key Mapping -> Map

Press {KeyMapHelper.GetKeyName(KeyTypes.Cancel)} to close this window.

If you wish to report issues or contribute to the TUI you can do so at: https://github.com/StefanHolst/opentap-tui/
",
                ReadOnly = true,
                Height = Dim.Fill(1),
            };
            Add(text);

            HelperButtons helperButtons = new HelperButtons()
            {
                Y = Pos.Bottom(text),
            };
            helperButtons.SetActions(new List<MenuItem>
            {
                new MenuItem("Help I'm bored", "", () => FocusMode.StartFocusMode(FocusModeUnlocks.HelpMenu, false), shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.HelperButton1))
            }, this);
            Add(helperButtons);
        }
    }
}

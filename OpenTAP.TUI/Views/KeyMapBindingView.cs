using System;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class KeyMapBindingView : View
    {
        public KeyEvent NewKeyMap { get; set; }

        public Action<bool> Closing;

        private TextView CurrentKeyText;
        private TextView NewKeyText;

        public KeyMapBindingView(KeyEvent currentKeyMap)
        {
            CanFocus = true;

            CurrentKeyText = new TextView
            {
                Text = $"Current key: {currentKeyMap}",
                CanFocus = false
            };
            Add(CurrentKeyText);

            NewKeyText = new TextView()
            {
                Y = 1,
                CanFocus = false
            };
            Add(NewKeyText);
        }

        public override bool ProcessKey(KeyEvent kb)
        {
            if (KeyMapHelper.IsKey(kb, KeyTypes.Select))
            {
                Closing(true);
                return false;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.Cancel))
            {
                Closing(false);
                return false;
            }

            // Record key press
            NewKeyMap = kb;
            NewKeyText.Text = $"New key: {NewKeyMap.Key}";
            return true;
        }
    }
}
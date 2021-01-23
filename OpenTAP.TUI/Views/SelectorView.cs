using System.Collections.Generic;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class SelectorView : View
    {
        private Label titleView;
        private ListView optionsView;
        
        public SelectorView(string title, List<string> availableOptions)
        {
            titleView = new Label(title)
            {
                Y = 1
            };
            
            optionsView = new ListView(availableOptions)
            {
                Width = Dim.Fill(),
                X = Pos.Right(titleView),
                Height = 3,
                AllowsMarking = true
            };
            optionsView.Source.SetMark(0, true);

            if (availableOptions.Count < 3)
                optionsView.Y = 1;
            
            Add(titleView);
            Add(optionsView);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Space)
            {
                for (int i = 0; i < optionsView.Source.Count; i++)
                    optionsView.Source.SetMark(i, false);
            }
            
            return base.ProcessKey(keyEvent);
        }
    }
}
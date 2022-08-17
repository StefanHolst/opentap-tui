using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class SelectorView : ListView
    {
        public Action<ListViewItemEventArgs> ItemMarkedChanged;
        public SelectorView()
        {
            AllowsMarking = true;
            AllowsMultipleSelection = true;
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            var handled = base.ProcessKey(keyEvent);
            if (keyEvent.Key == Key.Space)
            {
                ItemMarkedChanged?.Invoke(new ListViewItemEventArgs(SelectedItem, Source.ToList()[SelectedItem]));
            }

            return handled;
        }

        public List<object> MarkedItems()
        {
            var sourceList = Source.ToList();
            var list = new List<object>();
            for (var i = 0; i < Source.Count; i++)
            {
                if (Source.IsMarked(i))
                    list.Add(sourceList[i]);
            }
            
            return list;
        }
    }
}
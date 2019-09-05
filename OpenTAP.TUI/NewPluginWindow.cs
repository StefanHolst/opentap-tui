using System;
using System.Collections.ObjectModel;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class NewPluginWindow : Window
    {
        private ReadOnlyCollection<Type> Plugins { get; set; }
        private ListView listView { get; set; }
        public Type PluginType { get; set; }

        public NewPluginWindow(Type type, string title) : base(title)
        {
            Plugins = PluginManager.GetPlugins(type);
            listView = new ListView(Plugins);
            Add(listView);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
            {
                Running = false;
                return true;
            }

            if (keyEvent.Key == Key.Enter)
            {
                var index = listView.SelectedItem;
                if (Plugins.Count > 0)
                    PluginType = Plugins[index];
                Running = false;
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}
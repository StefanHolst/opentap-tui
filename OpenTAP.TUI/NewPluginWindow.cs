using System;
using System.Collections.ObjectModel;
using System.Linq;
using OpenTap;
using Terminal.Gui;
using System.Reflection;
using OpenTap.Tui;

namespace OpenTAP.TUI
{
    public class NewPluginWindow : EditWindow
    {
        private ReadOnlyCollection<Type> Plugins { get; set; }
        private TreeView treeview { get; set; }
        public Type PluginType { get; set; }

        public NewPluginWindow(Type type, string title) : base(title)
        {
            treeview = new TreeView(
                (item) => (item as Type).GetCustomAttribute<DisplayAttribute>()?.Name ?? (item as Type).Name, 
                (item) => (item as Type).GetCustomAttribute<DisplayAttribute>()?.Group);
            treeview.SetTreeViewSource(PluginManager.GetPlugins(type).ToList());
            Add(treeview);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter)
                PluginType = treeview.SelectedObject.obj as Type;

            return base.ProcessKey(keyEvent);
        }
    }
}
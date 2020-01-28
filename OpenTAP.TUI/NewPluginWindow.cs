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
        private ReadOnlyCollection<ITypeData> Plugins { get; set; }
        private TreeView treeview { get; set; }
        public ITypeData PluginType { get; set; }

        public NewPluginWindow(TypeData type, string title) : base(title)
        {
            treeview = new TreeView(
                (item) => (item as ITypeData).GetDisplayAttribute().Name ?? (item as ITypeData).Name, 
                (item) => (item as ITypeData).GetDisplayAttribute()?.Group);
            treeview.SetTreeViewSource(TypeData.GetDerivedTypes(type).Where(x => x.CanCreateInstance).ToList());
            Add(treeview);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter)
                PluginType = treeview.SelectedObject.obj as ITypeData;

            return base.ProcessKey(keyEvent);
        }
    }
}
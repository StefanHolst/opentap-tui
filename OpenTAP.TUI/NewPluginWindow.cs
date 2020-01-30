using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            
            var types = TypeData.GetDerivedTypes(type)
                .Where(x => x.CanCreateInstance)
                .Where(x => x.GetAttribute<BrowsableAttribute>()?.Browsable ?? true);
                
            treeview.SetTreeViewSource(types.ToList());
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
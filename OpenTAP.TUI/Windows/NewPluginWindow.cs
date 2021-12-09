using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using OpenTap;
using Terminal.Gui;
using OpenTap.Tui;

namespace OpenTap.Tui.Windows
{
    public class NewPluginWindow : EditWindow
    {
        private TreeView<ITypeData> treeview { get; set; }
        public ITypeData PluginType { get; set; }

        private string _originalTitle;

        public NewPluginWindow(TypeData type, string title) : base(title)
        {
            _originalTitle = title;
            treeview = new TreeView<ITypeData>(getTitle, getGroup);
            treeview.EnableFilter = true;
            treeview.FilterChanged += (filter) => { Title = string.IsNullOrEmpty(filter) ? _originalTitle : $"{_originalTitle} - {filter}"; };

            var types = TypeData.GetDerivedTypes(type)
                .Where(x => x.CanCreateInstance)
                .Where(x => x.GetAttribute<BrowsableAttribute>()?.Browsable ?? true);
                
            treeview.SetTreeViewSource(types.ToList());
            Add(treeview);
        }

        string getTitle(ITypeData item)
        {
            return item.GetDisplayAttribute().Name ?? item.Name;
        }
        List<string> getGroup(ITypeData item)
        {
            return item.GetDisplayAttribute()?.Group.ToList();
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter && treeview.SelectedObject != null)
                PluginType = treeview.SelectedObject;
            
            return base.ProcessKey(keyEvent);
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using OpenTap;
using Terminal.Gui;
using OpenTap.Tui;

namespace OpenTAP.TUI
{
    public class NewPluginWindow : EditWindow
    {
        private ReadOnlyCollection<ITypeData> Plugins { get; set; }
        private TreeView treeview { get; set; }
        public ITypeData PluginType { get; set; }

        private string _filter = "";
        private string _originalTitle;

        public string Filter
        {
            get { return _filter; }
            private set
            {
                if (_filter != value)
                {
                    _filter = value ?? "";
                    if (string.IsNullOrEmpty(_filter))
                    {
                        this.Title = _originalTitle;
                    }
                    else
                    {
                        this.Title = $"{_originalTitle} - {_filter}";
                    }
                    UpdateTreeView();
                }

            }
        }

        private void UpdateTreeView()
        {
            bool Predicate(TreeView.TreeViewItem item) => item.Title.ToLower().Contains(Filter.ToLower());
            treeview.SetVisible(Predicate);
            treeview.UpdateListView();
            treeview.SelectFirstMatch(Predicate);
        }

        public NewPluginWindow(TypeData type, string title) : base(title)
        {
            _originalTitle = title;
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
            if (keyEvent.Key == Key.Enter && treeview.SelectedObject?.obj != null)
            {
                PluginType = treeview.SelectedObject.obj as ITypeData;
                return base.ProcessKey(keyEvent);
            }

            if (keyEvent.KeyValue >= 32 && keyEvent.KeyValue < 127) // any non-special character is in this range
                Filter += (char) keyEvent.KeyValue;
            else if (keyEvent.Key == Key.Backspace && Filter.Length > 0)
                Filter = Filter.Substring(0, Filter.Length - 1);
            else if ((keyEvent.Key == Key.Backspace && keyEvent.IsCtrl) ||
                     keyEvent.Key == (Key.Backspace|Key.CtrlMask))
            {
                Filter = Filter.TrimEnd();
                var lastSpace = Filter.LastIndexOf(' ');
                var length = lastSpace > 0 ? lastSpace + 1 : 0;
                Filter = Filter.Substring(0, length);
            }
            else
                return base.ProcessKey(keyEvent);

            return true;
        }
    }
}
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

        private string filter
        {
            get { return _filter; }
            set
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
                treeview.SetVisible(item => item.Title.ToLower().Contains(_filter.ToLower()));
                treeview.UpdateListView();
            }
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
                PluginType = treeview.SelectedObject.obj as ITypeData;
            else if (keyEvent.KeyValue >= 32 && keyEvent.KeyValue < 127) // any non-special character is in this range
            {
                filter += (char) keyEvent.KeyValue;
            }
            else if ((keyEvent.Key & Key.Backspace) == Key.Backspace && filter.Length > 0)
            {
                var length = -1;
                if (keyEvent.IsCtrl)
                {
                    filter = filter.TrimEnd();
                    var lastSpace = filter.LastIndexOf(' ');
                    length = lastSpace > 0 ? lastSpace + 1 : 0;
                }
                else
                {
                    length = filter.Length - 1;
                }
                
                filter = filter.Substring(0, length);
            }
            
            return base.ProcessKey(keyEvent);
        }
    }
}
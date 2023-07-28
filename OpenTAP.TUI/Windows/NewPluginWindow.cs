using OpenTap.Tui.Views;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class NewPluginWindow : EditWindow
    {
        private TreeView<ITypeData> treeview { get; set; }
        private TextView descriptionView { get; set; }
        private FrameView descriptionFrame { get; set; }
        public ITypeData PluginType { get; set; }

        private string _originalTitle;

        public NewPluginWindow(TypeData type, string title, ITypeData parentType) : base(title)
        {
            _originalTitle = title;
            treeview = new TreeView<ITypeData>(getTitle, getGroup);
            treeview.EnableFilter = true;
            treeview.Height = Dim.Percent(90);
            treeview.SelectedItemChanged += (e) =>
            {
                var display = treeview.SelectedObject?.GetDisplayAttribute();
                var description = display?.Description;
                if (description != null)
                {
                    descriptionView.Text = PropertiesView.SplitText(description, descriptionView.Bounds.Width);
                }
                else
                    descriptionView.Text = "";
            };
            treeview.FilterChanged += (filter) => { Title = string.IsNullOrEmpty(filter) ? _originalTitle : $"{_originalTitle} - {filter}"; };

            // Description
            descriptionView = new TextView()
            {
                ReadOnly = true,
                AllowsTab = false
            };
            descriptionFrame = new FrameView(KeyMapHelper.GetKeyName(KeyTypes.FocusDescription, "Description"))
            {
                // X = 0,
                Y = Pos.Bottom(treeview),
                Height = Dim.Fill(),
                Width = Dim.Fill(),
                CanFocus = false
            };
            descriptionFrame.Add(descriptionView);

            var types = TypeData.GetDerivedTypes(type)
                .Where(x => x.CanCreateInstance)
                .Where(x => x.GetAttribute<BrowsableAttribute>()?.Browsable ?? true)
                .Where(x => parentType == null ? true : TestStepList.AllowChild(AsTypeData(parentType).Type, AsTypeData(x)?.Type));

            treeview.SetTreeViewSource(types.ToList());
            Add(treeview);
            Add(descriptionFrame);

            TypeData AsTypeData(ITypeData typedata)
            {
                do
                {
                    if (typedata is TypeData td)
                        return td;
                    typedata = typedata?.BaseType;
                } while (typedata != null);
                return null;
            }
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

            if (!treeview.HasFocus && keyEvent.Key == Key.Enter)
                return descriptionView.ProcessKey(keyEvent);

            return base.ProcessKey(keyEvent);
        }
    }
}
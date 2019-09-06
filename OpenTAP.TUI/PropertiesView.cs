using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class PropertiesView : View
    {
        public object obj { get; set; }
        public AnnotationCollection Annotation { get; set; }
        public ListView listView { get; set; } = new ListView();
        public TextView DescriptionView { get; set; } = new TextView();

        public PropertiesView()
        {
            listView.CanFocus = true;
            Add(listView);
            listView.Height = Dim.Percent(80);
            listView.SelectedChanged += ListViewOnSelectedChanged;

            DescriptionView.Y = Pos.Bottom(listView);
            Add(DescriptionView);
            DescriptionView.Text = "Description..";
        }

        private void ListViewOnSelectedChanged()
        {
            var members = getMembers();
            var description = members.ElementAtOrDefault(listView.SelectedItem)?.Get<DisplayAttribute>()?.Description;
            DescriptionView.Text = description ?? "";
        }

        public void LoadProperties(object obj)
        {
            this.obj = obj;
            Annotation = AnnotationCollection.Annotate(obj);
            UpdateProperties();
        }

        AnnotationCollection[] getMembers()
        {
            return Annotation.Get<IMembersAnnotation>().Members
                .Where(x => x.Get<IAccessAnnotation>()?.IsVisible ?? false)
                .ToArray();
        }
        private void UpdateProperties()
        {
            listView.SetSource(getMembers().Select(x => x.Get<DisplayAttribute>().Name).ToArray());
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter)
            {
                var index = listView.SelectedItem;
                var prop = getMembers();

                var propEditor = PropEditProvider.GetProvider(prop[index], out var provider);
                var win = new EditWindow(Annotation.ToString());
                if (propEditor == null)
                    propEditor = new TextView() {Text = "Unable to edit."};
                win.Add(propEditor);
                Application.Run(win);
                if(provider != null) provider.Commit(propEditor);
                Annotation.Write();
                Annotation.Read();
                UpdateProperties();
            }

            if (keyEvent.Key == Key.CursorRight)
                return true;

            return base.ProcessKey(keyEvent);
        }
    }
}
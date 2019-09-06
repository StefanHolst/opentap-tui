using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class PropertiesView : View
    {
        private object obj { get; set; }
        private AnnotationCollection annotations { get; set; }
        private ExtendedListView listView { get; set; } = new ExtendedListView();
        private TextView descriptionView { get; set; } = new TextView() { CanFocus = false };

        public PropertiesView()
        {
            listView.CanFocus = true;
            listView.Height = Dim.Percent(75);
            listView.SelectedChanged += ListViewOnSelectedChanged;
            Add(listView);

            // Description
            var descriptionFrame = new ExtendedFrameView("Description")
            {
                Y = Pos.Bottom(listView),
                Height = Dim.Fill(),
                Width = Dim.Fill(),
                CanFocus = false
            };
            descriptionFrame.Add(descriptionView);
            Add(descriptionFrame);

            listView.OnRedraw += (s, e) =>
            {
                ListViewOnSelectedChanged();
            };
        }

        private void ListViewOnSelectedChanged()
        {
            var members = getMembers();
            var description = members?.ElementAtOrDefault(listView.SelectedItem)?.Get<DisplayAttribute>()?.Description;
            
            if (description != null)
                descriptionView.Text = Regex.Replace(description, $".{{{descriptionView.Bounds.Width}}}", "$0\n");
            else
                descriptionView.Text = "";
        }

        public void LoadProperties(object obj)
        {
            this.obj = obj;
            annotations = AnnotationCollection.Annotate(obj);
            UpdateProperties();
            ListViewOnSelectedChanged();
        }

        AnnotationCollection[] getMembers()
        {
            return annotations?.Get<IMembersAnnotation>().Members
                .Where(x => x.Get<IAccessAnnotation>()?.IsVisible ?? false)
                .ToArray();
        }
        private void UpdateProperties()
        {
            var index = listView.SelectedItem;
            listView.SetSource(getMembers().Select(x => $"{x.Get<DisplayAttribute>().Name}: {x.Get<IStringValueAnnotation>()?.Value ?? "..."}").ToArray());
            listView.SelectedItem = index >= listView.Source.Count ? listView.Source.Count : index;
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter)
            {
                var members = getMembers();

                // Find edit provider
                var propEditor = PropEditProvider.GetProvider(members[listView.SelectedItem], out var provider);
                if (propEditor == null)
                    TUI.Log.Warning($"Cannot edit properties of type: {members[listView.SelectedItem].Get<IMemberAnnotation>().ReflectionInfo.Name}");
                else
                {
                    var win = new EditWindow(annotations.ToString());
                    win.Add(propEditor);
                    Application.Run(win);
                }

                // Save values to reference object
                annotations.Write();
                annotations.Read();

                // Load new values
                UpdateProperties();
            }

            if (keyEvent.Key == Key.CursorRight)
                return true;

            return base.ProcessKey(keyEvent);
        }
    }
}
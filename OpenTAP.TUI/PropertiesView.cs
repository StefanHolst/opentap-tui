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
        private object obj { get; set; }
        private AnnotationCollection annotations { get; set; }
        private ListView listView { get; set; } = new ListView();
        private TextView descriptionView { get; set; } = new TextView() { CanFocus = false };

        public PropertiesView()
        {
            listView.CanFocus = true;
            listView.Height = Dim.Percent(80);
            listView.SelectedChanged += ListViewOnSelectedChanged;
            Add(listView);

            // Description
            var descriptionFrame = new FrameView("Description")
            {
                Y = Pos.Bottom(listView),
                Height = Dim.Sized(6),
                CanFocus = false
            };
            descriptionFrame.Add(descriptionView);
            Add(descriptionFrame);
        }

        private void ListViewOnSelectedChanged()
        {
            var members = getMembers();
            var description = members.ElementAtOrDefault(listView.SelectedItem)?.Get<DisplayAttribute>()?.Description;
            descriptionView.Text = description ?? "";
        }

        public void LoadProperties(object obj)
        {
            this.obj = obj;
            annotations = AnnotationCollection.Annotate(obj);
            UpdateProperties();
        }

        AnnotationCollection[] getMembers()
        {
            return annotations.Get<IMembersAnnotation>().Members
                .Where(x => x.Get<IAccessAnnotation>()?.IsVisible ?? false)
                .ToArray();
        }
        private void UpdateProperties()
        {
            listView.SetSource(getMembers().Select(x => $"{x.Get<DisplayAttribute>().Name}: {x.Get<IStringValueAnnotation>()?.Value ?? "..."}").ToArray());
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter)
            {
                var members = getMembers();

                // Find edit provider
                var propEditor = PropEditProvider.GetProvider(members[listView.SelectedItem], out var provider);
                if (propEditor == null)
                    MessageBox.Query(40, 6, "Unable to edit", $"Cannot edit properties of type:\n{members[listView.SelectedItem].Get<IMemberAnnotation>().ReflectionInfo.Name}");
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
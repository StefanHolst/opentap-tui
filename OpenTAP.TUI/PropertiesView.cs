using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using OpenTap;
using OpenTAP.TUI.PropEditProviders;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class PropertiesView : View
    {
        private object obj { get; set; }
        private AnnotationCollection annotations { get; set; }
        private ListView listView { get; set; } = new ListView();
        private TextView descriptionView { get; set; } = new TextView();

        public PropertiesView()
        {
            listView.CanFocus = true;
            listView.Height = Dim.Percent(75);
            listView.SelectedChanged += ListViewOnSelectedChanged;
            Add(listView);

            // Description
            var descriptionFrame = new FrameView("Description")
            {
                Y = Pos.Bottom(listView),
                Height = Dim.Fill(),
                Width = Dim.Fill(),
                CanFocus = false
            };
            descriptionFrame.Add(descriptionView);
            Add(descriptionFrame);

            descriptionView.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ListView.Frame))
                {
                    ListViewOnSelectedChanged();
                }
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
            return annotations?.Get<IMembersAnnotation>()?.Members
                .Where(x => x.Get<IAccessAnnotation>()?.IsVisible ?? false)
                .Where(x => 
                {
                    var member = x.Get<IMemberAnnotation>()?.Member;
                    if (member != null)
                        return member.Attributes.Any(a => a is XmlIgnoreAttribute) == false && member.Writable;
                    else
                        return true;
                })
                .ToArray();
        }
        private void UpdateProperties()
        {
            var index = listView.SelectedItem;
            listView.SetSource(getMembers()?.Select(x => $"{x.Get<DisplayAttribute>().Name}: {x.Get<IStringValueAnnotation>()?.Value ?? x.Get<IObjectValueAnnotation>().Value}").ToArray());
            if (listView.Source?.Count == 0)
                return;
            listView.SelectedItem = index >= listView.Source?.Count ? listView.Source.Count : index;
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter)
            {
                var members = getMembers();
                if (members == null)
                    return false;

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

            if (keyEvent.Key == Key.CursorLeft || keyEvent.Key == Key.CursorRight)
                return true;

            if (keyEvent.Key == Key.F2)
            {
                SetFocus(descriptionView);
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}
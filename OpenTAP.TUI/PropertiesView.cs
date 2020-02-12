using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using OpenTap;
using OpenTap.TUI;
using OpenTAP.TUI.PropEditProviders;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class PropertiesView : View
    {
        private object obj { get; set; }
        private AnnotationCollection annotations { get; set; }
        private TreeView treeView { get; set; }
        private TextView descriptionView { get; set; }

        public event Action PropertiesChanged;
        
        public PropertiesView()
        {
            treeView = new TreeView(
                (item) =>
                {
                    var x = item as AnnotationCollection;
                    if (x == null)
                        return "";

                    var value = (x.Get<IStringReadOnlyValueAnnotation>()?.Value ?? x.Get<IAvailableValuesAnnotationProxy>()?.SelectedValue?.Source?.ToString() ?? x.Get<IObjectValueAnnotation>().Value)?.ToString() ?? "";
                    // replace new lines with spaces for viewing.
                    value = value.Replace("\n", " ").Replace("\r", "");

                    if (x.Get<IObjectValueAnnotation>()?.Value is Action)
                        return $"[ {x.Get<DisplayAttribute>().Name} ]";
                    
                    return $"{x.Get<DisplayAttribute>().Name}: {value}";
                }, 
                (item) => (item as AnnotationCollection).Get<DisplayAttribute>().Group);

            treeView.CanFocus = true;
            treeView.Height = Dim.Percent(75);
            treeView.SelectedChanged += ListViewOnSelectedChanged;
            Add(treeView);

            // Description
            descriptionView = new TextView()
            {
                ReadOnly = true
            };
            
            var descriptionFrame = new FrameView("Description")
            {
                Y = Pos.Bottom(treeView),
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
            var description = members?.ElementAtOrDefault(treeView.SelectedItem)?.Get<DisplayAttribute>()?.Description;
            
            if (description != null)
                descriptionView.Text = Regex.Replace(description, $".{{{descriptionView.Bounds.Width}}}", "$0\n");
            else
                descriptionView.Text = "";
        }

        public void LoadProperties(object obj)
        {
            this.obj = obj;
            annotations = AnnotationCollection.Annotate(obj);
            var members = getMembers();
            if (members == null)
                members = new AnnotationCollection[0];
            treeView.SetTreeViewSource<AnnotationCollection>(members.ToList());
            ListViewOnSelectedChanged();
        }

        static public bool FilterMember(IMemberData member)
        {
            if (member.GetAttribute<BrowsableAttribute>()?.Browsable ?? false)
                return true;
            return member.Attributes.Any(a => a is XmlIgnoreAttribute) == false && member.Writable;
        }
    
        AnnotationCollection[] getMembers()
        {
            return annotations?.Get<IMembersAnnotation>()?.Members
                .Where(x => x.Get<IAccessAnnotation>()?.IsVisible ?? false)
                .Where(x => 
                {
                    var member = x.Get<IMemberAnnotation>()?.Member;
                    if (member == null) return false;
                    return FilterMember(member);
                })
                .ToArray();
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter && treeView.SelectedObject?.obj != null)
            {
                var members = getMembers();
                if (members == null)
                    return false;

                // Find edit provider
                var member = treeView.SelectedObject.obj as AnnotationCollection;
                var propEditor = PropEditProvider.GetProvider(member, out var provider);
                if (propEditor == null)
                    TUI.Log.Warning($"Cannot edit properties of type: {member.Get<IMemberAnnotation>().ReflectionInfo.Name}");
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
                LoadProperties(obj);
                
                // Invoke property changed event
                PropertiesChanged?.Invoke();
            }

            if (keyEvent.Key == Key.CursorLeft || keyEvent.Key == Key.CursorRight)
            {
                treeView.ProcessKey(keyEvent);
                return true;
            }

            if (keyEvent.Key == Key.F1)
            {
                treeView.FocusFirst();
                return true;
            }
            if (keyEvent.Key == Key.F2)
            {
                SetFocus(descriptionView);
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}
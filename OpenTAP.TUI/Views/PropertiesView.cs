using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using OpenTap.Tui.PropEditProviders;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class PropertiesView : View
    {
        private object obj { get; set; }
        private AnnotationCollection annotations { get; set; }
        private TreeView treeView { get; set; }
        private TextView descriptionView { get; set; }
        private FrameView descriptionFrame { get; set; }
        private View submitView { get; set; }

        public event Action PropertiesChanged;
        public event Action Submit;
        
        public PropertiesView()
        {
            treeView = new TreeView(
                (item) =>
                {
                    var x = item as AnnotationCollection;
                    if (x == null)
                        return "";

                    var value = ((x.Get<IAvailableValuesAnnotation>() as IStringReadOnlyValueAnnotation)?.Value ?? x.Get<IStringReadOnlyValueAnnotation>()?.Value ?? x.Get<IAvailableValuesAnnotationProxy>()?.SelectedValue?.Source?.ToString() ?? x.Get<IObjectValueAnnotation>().Value)?.ToString() ?? "...";
                    // replace new lines with spaces for viewing.
                    value = value.Replace("\n", " ").Replace("\r", "");

                    if (x.Get<IObjectValueAnnotation>()?.Value is Action)
                        return $"[ {x.Get<DisplayAttribute>().Name} ]";

                    // Don't show member name if layout is fullrow
                    if (x.Get<IMemberAnnotation>()?.Member.GetAttribute<LayoutAttribute>()?.Mode == LayoutMode.FullRow)
                        return value;
                    
                    return $"{x.Get<DisplayAttribute>().Name}: {value}";
                }, 
                (item) => (item as AnnotationCollection).Get<DisplayAttribute>().Group);

            treeView.CanFocus = true;
            treeView.Height = Dim.Percent(75);
            treeView.SelectedItemChanged += ListViewOnSelectedChanged;
            Add(treeView);

            // Description
            descriptionView = new TextView()
            {
                ReadOnly = true,
            };
            descriptionFrame = new FrameView("Description")
            {
                // X = 0,
                Y = Pos.Bottom(treeView),
                Height = Dim.Fill(),
                Width = Dim.Fill(),
                CanFocus = false
            };
            descriptionFrame.Add(descriptionView);
            Add(descriptionFrame);

            // Submit buttons view
            submitView = new View()
            {
                Height = 1,
                Width = Dim.Fill(),
                Y = Pos.Bottom(descriptionFrame)
            };
            Add(submitView);

            // Make sure we redraw everything after we have loaded everything. Just to make sure we have the right sizes.
            LayoutComplete += args =>
            {
                treeView.UpdateListView();
                ListViewOnSelectedChanged(null);
            };
        }

        List<Button> getSubmitButtons()
        {
            // Get submit buttons
            var buttons = new List<Button>();
            var members = annotations?.Get<IMembersAnnotation>()?.Members?.ToList();
            var submit = members?.FirstOrDefault(m => m.Get<IAccessAnnotation>().IsVisible && m.Get<IMemberAnnotation>()?.Member.GetAttribute<SubmitAttribute>() != null);
            if (submit != null)
            {
                var availableValuesAnnotation = submit.Get<IAvailableValuesAnnotationProxy>();
                foreach (var availableValue in availableValuesAnnotation.AvailableValues)
                {
                    var button = new Button(availableValue.Source.ToString(), availableValuesAnnotation.SelectedValue == availableValue)
                    {
                        Clicked = () =>
                        {
                            availableValuesAnnotation.SelectedValue = availableValue;
                            submit.Write();
                            Submit();
                        }
                    };

                    
                    buttons.Add(button);
                }
            }

            return buttons;
        }

        private void ListViewOnSelectedChanged(ListViewItemEventArgs args)
        {
            var description = (treeView.SelectedObject?.obj as AnnotationCollection)?.Get<DisplayAttribute>()?.Description;
            
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

            // Only show description view if there are any properties with descriptions
            descriptionFrame.Visible = members.Any(a => a.Get<DisplayAttribute>()?.Description != null);

            // Add submit buttons
            var submitButtons = getSubmitButtons();
            if (submitButtons.Any())
            {
                descriptionFrame.Height = Dim.Fill(1);
                submitView.RemoveAll();
                submitView.Add(submitButtons.ToArray());
                
                // Center buttons
                var buttonsTotalWidth = submitButtons.Select(b => b.Bounds.Width).Sum() + submitButtons.Count() - 1;
                submitView.Width = buttonsTotalWidth;
                submitView.X = Pos.Center();
                for (int i = 1; i < submitButtons.Count; i++)
                    submitButtons[i].X = Pos.Right(submitButtons[i - 1]) + 1;
            }
            else
                descriptionFrame.Height = Dim.Fill();
            
            treeView.SetTreeViewSource(members.ToList());
        }

        public static bool FilterMember(IMemberData member)
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
                    if (member == null || member.GetAttribute<SubmitAttribute>() != null) return false;
                    return FilterMember(member);
                })
                .ToArray();
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (MostFocused is TreeView && keyEvent.Key == Key.Enter && treeView.SelectedObject?.obj != null)
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

                return true;
            }

            if (MostFocused is TreeView && (keyEvent.Key == Key.CursorLeft || keyEvent.Key == Key.CursorRight))
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
                descriptionView.SetFocus(); //TODO: test
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}
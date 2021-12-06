using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
        private TreeView2<AnnotationCollection> treeView { get; set; }
        private TextView descriptionView { get; set; }
        private FrameView descriptionFrame { get; set; }
        private View submitView { get; set; }
        internal bool DisableHelperButtons { get; set; }

        public Action<string> TreeViewFilterChanged { get; set; }

        static readonly TraceSource log = Log.CreateSource("tui");
        
        void buildMenuItems(AnnotationCollection selectedMember)
        {
            // Only update the helperbuttons if we have focus
            if (HasFocus == false || DisableHelperButtons)
                return;
            
            var list = new List<MenuItem>();
            
            var menu = selectedMember?.Get<MenuAnnotation>();
            if (menu == null)
            {
                MainWindow.helperButtons.SetActions(list, this);
                return;
            }
            
            foreach (var _member in menu.MenuItems)
            {
                var member = _member;
                if (member.Get<IAccessAnnotation>()?.IsVisible == false)
                    continue;
                
                var item = new MenuItem();
                item.Title = member.Get<DisplayAttribute>().Name;
                item.Action = () =>
                {
                    try
                    {
                        member.Get<IMethodAnnotation>().Invoke();
                    }
                    catch (Exception e)
                    {
                        log.Error("Error executing action: {0}", e.Message);
                        log.Debug(e);
                    }

                    try
                    {
                        LoadProperties(obj);
                    }
                    catch {  }
                };
                item.CanExecute = () => member.Get<IEnabledAnnotation>()?.IsEnabled != false;
                list.Add(item);
            }

            MainWindow.helperButtons.SetActions(list, this);
        }

        public event Action PropertiesChanged;
        public event Action Submit;
        
        public PropertiesView(bool EnableFilter = false)
        {
            treeView = new TreeView2<AnnotationCollection>(getTitle, getGroup);
            treeView.CanFocus = true;
            treeView.Height = Dim.Percent(75);
            treeView.SelectedItemChanged += ListViewOnSelectedChanged;
            treeView.OpenSelectedItem += OpenSelectedItem;
            treeView.EnableFilter = EnableFilter;
            treeView.FilterChanged += (f) => TreeViewFilterChanged?.Invoke(f);
            Add(treeView);

            // Description
            descriptionView = new TextView()
            {
                ReadOnly = true,
                AllowsTab = false
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
                treeView.RenderTreeView();
                ListViewOnSelectedChanged(null);
            };

            Enter += args =>
            {
                ListViewOnSelectedChanged(null);
            };
        }

        private void OpenSelectedItem(ListViewItemEventArgs listViewItemEventArgs)
        {
            var members = getMembers();
            if (members == null)
                return;

            // Find edit provider
            var member = treeView.SelectedObject;
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

        string getTitle(AnnotationCollection x)
        {
            if (x == null)
                return "";

            var nameBuilder = new StringBuilder();
            var value = ((x.Get<IAvailableValuesAnnotation>() as IStringReadOnlyValueAnnotation)?.Value 
                         ?? x.Get<IStringReadOnlyValueAnnotation>()?.Value 
                         ?? x.Get<IAvailableValuesAnnotationProxy>()?.SelectedValue?.Source?.ToString() 
                         ?? x.Get<IObjectValueAnnotation>()?.Value)?.ToString() 
                        ?? "...";
            // replace new lines with spaces for viewing.
            value = value.Replace("\n", " ").Replace("\r", "");

            if (x.Get<IObjectValueAnnotation>()?.Value is Action)
                return $"[ {x.Get<DisplayAttribute>().Name} ]";

            // Don't show member name if layout is fullrow
            if (x.Get<IMemberAnnotation>()?.Member.GetAttribute<LayoutAttribute>()?.Mode == LayoutMode.FullRow)
                return value;
            var icons = x.GetAll<IIconAnnotation>().ToArray();
            var icons2 = new HashSet<string>(icons.Select(y => y.IconName));
            bool icon(string name) => icons2.Contains(name);
            nameBuilder.Clear();
            if (icon(IconNames.OutputAssigned))
                nameBuilder.Append((char)Driver.Selected); // ●
            else if (icon(IconNames.Output))
                nameBuilder.Append((char)Driver.UnSelected); // ⃝
            if (icon(IconNames.Input))
            {
                nameBuilder.Append((char)Driver.Selected); // ●
                nameBuilder.Append((char)Driver.RightArrow); // →
            }
            if(icon(IconNames.Parameterized))
                nameBuilder.Append((char)Driver.Lozenge);// ♦
            if (x.Get<IMemberAnnotation>()?.Member is IParameterMemberData)
                nameBuilder.Append((char)Driver.Diamond);// ◊

            if (nameBuilder.Length > 0)
                nameBuilder.Append(" ");
            
            nameBuilder.Append(x.Get<DisplayAttribute>().Name);
            nameBuilder.Append(": ");
            nameBuilder.Append(value);

            // Check validation rules
            var step = x.Source as IValidatingObject;
            var propertyName = x.Get<IMemberAnnotation>()?.Member?.Name;
            var rule = step?.Rules.FirstOrDefault(r => r.PropertyName == propertyName && r?.IsValid() == false);
            if (rule != null)
                nameBuilder.Append(" !");
            
            return nameBuilder.ToString();
        }

        List<string> getGroup(AnnotationCollection annotationCollection)
        {
            return annotationCollection?.Get<DisplayAttribute>().Group.ToList();
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
                    var button = new Button(availableValue.Source.ToString(), availableValuesAnnotation.SelectedValue == availableValue);
                    button.Clicked += () =>
                    {
                        availableValuesAnnotation.SelectedValue = availableValue;
                        submit.Write();
                        Submit();
                    };
                    
                    buttons.Add(button);
                }
            }

            return buttons;
        }

        private void ListViewOnSelectedChanged(ListViewItemEventArgs args)
        {
            var memberAnnotation = treeView.SelectedObject;
            var display = memberAnnotation?.Get<DisplayAttribute>();
            var description = display?.Description;

            // Check validation rules
            if (memberAnnotation != null)
            {
                var step = memberAnnotation.Source as IValidatingObject;
                var rules = step?.Rules.Where(r => r.PropertyName == display?.Name && r?.IsValid() == false).ToList();
                if (rules?.Any() == true)
                {
                    var messages = rules.Select(r => r.ErrorMessage);
                    description = $"! {string.Join("\n", messages)}\n{new String('-', descriptionView.Bounds.Width - 1)}\n{description}";
                }
            }

            if (description != null)
                descriptionView.Text = SplitText(description, descriptionView.Bounds.Width);
            else
                descriptionView.Text = "";
            

            buildMenuItems(memberAnnotation);
        }

        public static string SplitText(string text, int length)
        {
            if (length < 2)
                return text;
            StringBuilder output = new StringBuilder();

            while (text.Length > length)
            {
                for (int i = length; i >= length/2; i--)
                {
                    if (char.IsWhiteSpace(text[i]))
                    {
                        output.AppendLine(text.Substring(0, i).Trim());
                        text = text.Substring(i);
                        break;
                    }

                    if (length/2 == i)
                    {
                        output.AppendLine(text.Substring(0, length).Trim());
                        text = text.Substring(length);
                    }
                }
            }

            output.AppendLine(text.Trim());
            return output.ToString().Replace("\r", "");
        }

        public void LoadProperties(object obj)
        {
            this.obj = obj ?? new object();
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
            if (member.GetAttribute<BrowsableAttribute>() is BrowsableAttribute attr)
                 return attr.Browsable;
            if (member.HasAttribute<OutputAttribute>())
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
    }
}
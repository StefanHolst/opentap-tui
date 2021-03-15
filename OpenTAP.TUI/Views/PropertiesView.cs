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
        private TreeView treeView { get; set; }
        private TextView descriptionView { get; set; }
        private FrameView descriptionFrame { get; set; }
        private View submitView { get; set; }

        public List<MenuItem> ActiveMenuItems { get; private set; } = new List<MenuItem>();

        public Action SelectionChanged { get; set; }

        static readonly TraceSource log = Log.CreateSource("tui");
        
        void buildMenuItems(AnnotationCollection selectedMember)
        {
            
            ActiveMenuItems.Clear();
            
            var menu = selectedMember?.Get<MenuAnnotation>();
            if (menu == null) return;
            
            foreach (var _member in menu.MenuItems)
            {
                var member = _member;
                if (member.Get<IAccessAnnotation>()?.IsVisible == false)
                    continue;

                if (ActiveMenuItems.Count == 0)
                {
                    var item2 = new MenuItem {Title = $"--- {selectedMember.Get<DisplayAttribute>().Name} ---"};
                    ActiveMenuItems.Add(item2);

                }
                
                var item = new MenuItem();
                item.Title = member.Get<DisplayAttribute>().Name;
                item.Action = () =>
                {
                    if (member.Get<IEnabledAnnotation>()?.IsEnabled == false)
                    {
                        log.Info("'{0}' is not curently enabled.", item.Title);
                        return;
                    }
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
                item.CanExecute = () => true;
                ActiveMenuItems.Add(item);
            }
        }

        public event Action PropertiesChanged;
        public event Action Submit;
        
        public PropertiesView()
        {
            StringBuilder nameBuilder = new StringBuilder();

            treeView = new TreeView(
                (item) =>
                {
                    var x = item as AnnotationCollection;
                    if (x == null)
                        return "";

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
                    var icons2 = new HashSet<string>(icons.Select(y => y.IconName));//(y => y.IconName == OpenTap.IconNames.Parameterized);
                    bool icon(string name) => icons2.Contains(name);
                    nameBuilder.Clear();
                    if(icon(IconNames.OutputAssigned))
                        nameBuilder.Append("●");
                    else if(icon(IconNames.Output))
                        nameBuilder.Append("⭘");
                    if(icon(IconNames.Input))
                        nameBuilder.Append("●→");
                    if(icon(IconNames.Parameterized))
                        nameBuilder.Append("◇");
                    if(x.Get<IMemberAnnotation>()?.Member is IParameterMemberData)
                        nameBuilder.Append("◆");

                    var propertyName = x.Get<DisplayAttribute>().Name;
                    nameBuilder.Append(propertyName);
                    nameBuilder.Append(": ");
                    nameBuilder.Append(value);

                    // Check validation rules
                    var step = x.Source as TestStep;
                    var rule = step?.Rules.FirstOrDefault(r => r.PropertyName == propertyName && r?.IsValid() == false);
                    if (rule != null)
                        nameBuilder.Append(" !");
                    
                    return nameBuilder.ToString();
                }, 
                (item) => (item as AnnotationCollection)?.Get<DisplayAttribute>().Group);

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
            var memberAnnotation = treeView.SelectedObject?.obj as AnnotationCollection;
            var display = memberAnnotation?.Get<DisplayAttribute>();
            var description = display?.Description;
            var propertyName = display?.Name;

            // Check validation rules
            if (memberAnnotation != null)
            {
                var step = memberAnnotation.Source as TestStep;
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
            SelectionChanged?.Invoke();
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
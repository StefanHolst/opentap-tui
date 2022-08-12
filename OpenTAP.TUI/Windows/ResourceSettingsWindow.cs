using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using OpenTap.Tui.PropEditProviders;
using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class ResourceSettingsWindow : Window
    {
        public IList Resources { get; set; }
        private List<string> list { get; set; }
        private ListView listView { get; set; }
        private Button addButton { get; set; }
        private PropertiesView detailsView { get; set; } = new PropertiesView();

        /// <summary>
        /// Return the resource name if it is defined, otherwise give a reasonable fallback name
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private string getResourceName(IResource r)
        {
            if (string.IsNullOrWhiteSpace(r.Name))
                return "Unnamed Resource: " + (TypeData.GetTypeData(r).GetDisplayAttribute()?.GetFullName() ?? r.GetType().FullName);
            return r.Name;
        }

        public ResourceSettingsWindow(string title, IList resources) : base(null)
        {
            Modal = true;
            this.Resources = resources;
            
            // list frame
            var frame = new FrameView(title)
            {
                Width = Dim.Percent(25),
                Height = Dim.Fill()
            };

            // resource list
            list = Resources.Cast<IResource>().Select(getResourceName).ToList();
            listView = new ListView(list)
            {
                Height = Dim.Fill(1)
            };
            
            listView.SelectedItemChanged += args => 
            {
                if (Resources.Count == 0)
                    return;

                var resource = Resources[args.Item];
                detailsView.LoadProperties(resource);
            };
            frame.Add(listView);

            // add resource button
            addButton = new Button("+")
            {
                Width = Dim.Fill(),
                Y = Pos.Bottom(listView)
            };
            addButton.Clicked += () =>
            {
                var newPlugin = new NewPluginWindow(TypeData.FromType(DataGridEditProvider.GetEnumerableElementType(Resources.GetType())), title, null);
                Application.Run(newPlugin);
                if (newPlugin.PluginType != null)
                {
                    try
                    {
                        var resource = newPlugin.PluginType.CreateInstance();
                        Resources.Add(resource);
                        listView.SetSource(Resources.Cast<IResource>().Select(getResourceName).ToList());
                        if (Resources.Count == 1)
                            detailsView.LoadProperties(resource);
                    }
                    catch (Exception ex)
                    {
                        ComponentSettings.SaveAllCurrentSettings();
                        TUI.Log.Error(ex);
                        Application.RequestStop();
                    }
                }
            };
            frame.Add(addButton);

            // details frame
            var detailFrame = new FrameView("Details")
            {
                X = Pos.Percent(25),
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            detailFrame.Add(detailsView);
            if (Resources.Count > 0)
                detailsView.LoadProperties(Resources[0]);

            Add(frame);
            Add(detailFrame);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.DeleteChar && listView.HasFocus)
            {
                var index = listView.SelectedItem;

                if (Resources.Count == 0 || listView.SelectedItem == -1)
                    return false;

                Resources.RemoveAt(listView.SelectedItem);
                listView.SetSource(Resources.Cast<IResource>().Select(getResourceName).ToList());

                if (Resources.Count > 0)
                {
                    listView.SelectedItem = (index > Resources.Count - 1 ? Resources.Count - 1 : index);
                    detailsView.LoadProperties(Resources[listView.SelectedItem]);
                }
                else
                {
                    detailsView.LoadProperties(null);
                }

                return true;
            }

            if (keyEvent.Key == Key.Esc)
            {
                ComponentSettings.SaveAllCurrentSettings();
                Application.RequestStop();
                return true;
            }

            if (keyEvent.Key == Key.F1)
            {
                listView.FocusFirst();
                return true;
            }
            if (keyEvent.Key == Key.F2)
            {
                detailsView.FocusFirst();
                return true;
            }
            if (keyEvent.Key == Key.F3)
            {
                detailsView.FocusLast();
                return true;
            }
            if (keyEvent.Key == Key.F4)
            {
                addButton.FocusFirst();
                return true;
            }
            
            return base.ProcessKey(keyEvent);
        }
    }
}
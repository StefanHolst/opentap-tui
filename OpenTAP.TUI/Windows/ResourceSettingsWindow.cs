using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using OpenTap;
using OpenTAP.TUI.PropEditProviders;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class ResourceSettingsWindow : Window
    {
        public IList Resources { get; set; }
        private List<string> list { get; set; }
        private ListView listView { get; set; }
        private Button addButton { get; set; }
        private PropertiesView detailsView { get; set; } = new PropertiesView();

        public ResourceSettingsWindow(string title, IList resources) : base(null)
        {
            this.Resources = resources;
            
            // list frame
            var frame = new FrameView(title)
            {
                Width = Dim.Percent(25),
                Height = Dim.Fill()
            };
            
            // resource list
            list = Resources.Cast<IResource>().Select(r => r.Name).ToList();
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
                var newPlugin = new NewPluginWindow(TypeData.FromType(DataGridEditProvider.GetEnumerableElementType(Resources.GetType())), title);
                Application.Run(newPlugin);
                if (newPlugin.PluginType != null)
                {
                    try
                    {
                        var resource = newPlugin.PluginType.CreateInstance();
                        Resources.Add(resource);
                        listView.SetSource(Resources.Cast<IResource>().Select(r => r.Name).ToList());
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
            if (keyEvent.Key == Key.DeleteChar)
            {
                var index = listView.SelectedItem;

                if (Resources.Count == 0 || listView.SelectedItem == -1)
                    return false;

                Resources.RemoveAt(listView.SelectedItem);
                listView.SetSource(Resources.Cast<IResource>().Select(r => r.Name).ToList());

                if (Resources.Count > 0)
                {
                    listView.SelectedItem = (index > Resources.Count - 1 ? Resources.Count - 1 : index);
                    detailsView.LoadProperties(Resources[listView.SelectedItem]);
                }
                else{
                    detailsView.LoadProperties(null);
                }
            }

            if (keyEvent.Key == Key.Esc)
            {
                ComponentSettings.SaveAllCurrentSettings();
                Application.RequestStop();
                return true;
            }

            if (keyEvent.Key == Key.Tab || keyEvent.Key == Key.BackTab)
            {
                if (listView.HasFocus)
                    detailsView.FocusFirst();
                else
                    listView.FocusFirst();

                return true;
            }

            if (keyEvent.Key == Key.CursorDown)
            {
                if (addButton.HasFocus)
                    return true;

                var index = listView.SelectedItem;
                base.ProcessKey(keyEvent);
                if (listView.HasFocus && index == listView.SelectedItem)
                {
                    addButton.FocusFirst();
                }

                return true;
            }

            if (keyEvent.Key == Key.CursorRight || keyEvent.Key == Key.CursorLeft)
            {
                if (detailsView.HasFocus)
                    detailsView.ProcessKey(keyEvent);
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
                addButton.FocusFirst();
                return true;
            }
            if (keyEvent.Key == Key.F4)
            {
                var kevent = keyEvent;
                kevent.Key = Key.F2;
                detailsView.ProcessKey(kevent);
                return true;
            }
            
            return base.ProcessKey(keyEvent);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class ResourceSettingsWindow<T> : Window where T : IResource
    {
        public IList Resources { get; set; }
        private List<string> list { get; set; }
        private ListView listView { get; set; }
        private PropertiesView detailsView { get; set; } = new PropertiesView();

        public ResourceSettingsWindow(string title) : base(null)
        {
            Resources = ComponentSettingsList.GetContainer(typeof(T));

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
            listView.SelectedChanged += () =>
            {
                var resource = Resources[listView.SelectedItem];
                detailsView.LoadProperties(resource);
            };
            frame.Add(listView);

            // add resource button
            var button = new Button("+")
            {
                Width = Dim.Fill(),
                Y = Pos.Bottom(listView)
            };
            button.Clicked += () =>
            {
                var newPlugin = new NewPluginWindow(typeof(T), title);
                Application.Run(newPlugin);
                if (newPlugin.PluginType != null)
                {
                    var resource = Activator.CreateInstance(newPlugin.PluginType);
                    Resources.Add(resource);
                    listView.SetSource(Resources.Cast<IResource>().Select(r => r.Name).ToList());
                    if (Resources.Count == 1)
                        detailsView.LoadProperties(resource);
                }
            };
            frame.Add(button);

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
                Resources.RemoveAt(listView.SelectedItem);
                listView.SetSource(Resources.Cast<IResource>().Select(r => r.Name).ToList());

                if (Resources.Count > 0)
                {
                    listView.SelectedItem = (index > Resources.Count - 1 ? Resources.Count - 1 : index);
                    detailsView.LoadProperties(Resources[listView.SelectedItem]);
                }
            }

            if (keyEvent.Key == Key.Esc)
            {
                ComponentSettings.SaveAllCurrentSettings();
                Application.RequestStop();
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}
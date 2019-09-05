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
        public object obj { get; set; }
        public List<PropertyInfo> properties { get; set; }
        public ListView listView { get; set; } = new ListView();

        public PropertiesView()
        {
            listView.CanFocus = true;
            Add(listView);
        }

        public void LoadProperties(object obj)
        {
            this.obj = obj;
            properties = obj.GetType().GetProperties().Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false && p.SetMethod?.IsPublic == true && p.GetCustomAttribute<XmlIgnoreAttribute>() == null).ToList();
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            listView.SetSource(properties.Select(p => $"{p.Name}: {p.GetValue(obj)?.ToString()}").ToList());
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter)
            {
                var index = listView.SelectedItem;
                var prop = properties[index];

                var propEditor = PropEditProvider.GetProvider(prop);
                propEditor.Edit(prop, obj);

                UpdateProperties();
            }

            if (keyEvent.Key == Key.CursorRight)
                return true;

            return base.ProcessKey(keyEvent);
        }
    }
}
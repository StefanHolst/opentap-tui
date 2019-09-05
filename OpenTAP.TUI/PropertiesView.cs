using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using OpenTap;
using Terminal.Gui;

public class PropertiesView : View
    {
        public object Value { get; set; }
        public List<PropertyInfo> properties { get; set; }
        public ListView listView { get; set; } = new ListView();

        public PropertiesView()
        {
            listView.CanFocus = true;
            Add(listView);
        }

        public void LoadProperties(object obj)
        {
            Value = obj;
            properties = obj.GetType().GetProperties().Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false && p.SetMethod?.IsPublic == true && p.GetCustomAttribute<XmlIgnoreAttribute>() == null).ToList();

            listView.SetSource(properties.Select(p => $"{p.Name}: {p.GetValue(Value)?.ToString()}").ToList());
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter)
            {
                var index = listView.SelectedItem;
                var prop = properties[index];
                var setting = new PropEditWindow(prop, prop.GetValue(Value));
                Application.Run(setting);
                if (setting.Value != null)
                    prop.SetValue(Value, setting.Value);

                listView.SetSource(properties.Select(p => $"{p.Name}: {p.GetValue(Value)?.ToString()}").ToList());
            }

            if (keyEvent.Key == Key.CursorRight)
                return true;

            return base.ProcessKey(keyEvent);
        }
    }

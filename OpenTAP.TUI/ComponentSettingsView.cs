using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using OpenTap;
using Terminal.Gui;

public class ComponentSettingsView : Window
{
    private ComponentSettings setting { get; set; }
    private List<PropertyInfo> props { get; set; }
    private ListView listView { get; set; }

    public ComponentSettingsView(ComponentSettings setting) : base(setting.GetType().GetCustomAttribute<DisplayAttribute>().Name)
    {
        this.setting = setting;
        props = setting.GetType().GetProperties().Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false && p.SetMethod?.IsPublic == true && p.GetCustomAttribute<XmlIgnoreAttribute>() == null).ToList();
        listView = new ListView(props.Select(p => p.Name).ToList());
        Add(listView);
    }

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        if (keyEvent.Key == Key.Esc)
        {
            Running = false;
            return true;
        }

        if (keyEvent.Key == Key.Enter)
        {
            var index = listView.SelectedItem;
            var prop = props[index];
            var propView = new PropEditView(prop, prop.GetValue(setting));
            Application.Run(propView);
            if (propView.Value != null)
            {
                prop.SetValue(setting, propView.Value);
                setting.Save();
            }
            return true;
        }

        return base.ProcessKey(keyEvent);
    }
}
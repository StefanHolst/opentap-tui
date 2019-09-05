using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using OpenTap;
using Terminal.Gui;

public class ComponentSettingsWindow : Window
{
    private ComponentSettings setting { get; set; }

    public ComponentSettingsWindow(ComponentSettings setting) : base(setting.GetType().GetCustomAttribute<DisplayAttribute>().Name)
    {
        this.setting = setting;
        var propView = new PropertiesView();
        propView.LoadProperties(setting);
        Add(propView);
    }

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        if (keyEvent.Key == Key.Esc)
        {
            setting.Save();
            Running = false;
            return true;
        }

        return base.ProcessKey(keyEvent);
    }
}
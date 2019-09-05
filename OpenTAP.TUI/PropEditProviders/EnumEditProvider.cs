using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class EnumEditProvider : IPropEditProvider
    {
        public int Order => 0;

        public bool CanEdit(PropertyInfo prop)
        {
            return prop.PropertyType.IsEnum;
        }

        public void Edit(PropertyInfo prop, object obj)
        {
            var availableValues = Enum.GetValues(prop.PropertyType);

            var win = new EditWindow(prop.Name);
            var listView = new ListView(availableValues);
            win.Add(listView);

            Application.Run(win);

            if (win.Edited && availableValues.Length > 0)
                prop.SetValue(obj, availableValues.GetValue(listView.SelectedItem));
        }
    }
}

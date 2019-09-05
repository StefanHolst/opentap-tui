using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class DefaultEditProvider : IPropEditProvider
    {
        public int Order => 1000;

        public bool CanEdit(PropertyInfo prop)
        {
            return TypeDescriptor.GetConverter(prop.PropertyType).CanConvertFrom(typeof(string));
        }

        public void Edit(PropertyInfo prop, object obj)
        {
            var win = new EditWindow(prop.Name);
            var textField = new TextField(prop.GetValue(obj).ToString());

            win.Add(textField);

            Application.Run(win);

            if (win.Edited)
            {
                var value = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFrom(textField.Text.ToString());
                prop.SetValue(obj, value);
            }
        }
    }
}

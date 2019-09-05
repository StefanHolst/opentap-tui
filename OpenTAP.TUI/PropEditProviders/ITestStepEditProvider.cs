using OpenTap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class ITestStepEditProvider : IPropEditProvider
    {
        public int Order => 0;

        public bool CanEdit(PropertyInfo prop)
        {
            return prop.PropertyType.IsAssignableFrom(typeof(ITestStep));
        }

        public void Edit(PropertyInfo prop, object obj)
        {
            var plugins = PluginManager.GetPlugins(prop.PropertyType);

            var win = new EditWindow(prop.Name);
            var listView = new ListView(plugins);
            win.Add(listView);

            Application.Run(win);

            if (win.Edited && plugins.Any())
                prop.SetValue(obj, Activator.CreateInstance(plugins[listView.SelectedItem]) as ITestStep);
        }
    }
}

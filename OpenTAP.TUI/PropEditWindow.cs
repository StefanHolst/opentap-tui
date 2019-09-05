using OpenTap;
using OpenTAP.TUI.PropEditProviders;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Terminal.Gui;


namespace OpenTAP.TUI
{
    public static class PropEditProvider
    {
        public static IPropEditProvider GetProvider(PropertyInfo prop)
        {
            var editProviders = PluginManager.GetPlugins<IPropEditProvider>().Select(p => Activator.CreateInstance(p) as IPropEditProvider).OrderBy(p => p.Order).ToList();

            foreach (var item in editProviders)
            {
                if (item.CanEdit(prop))
                    return item;
            }

            return null;
        }
    }

    public class EditWindow : Window
    {
        public bool Edited { get; set; }

        public EditWindow(string title) : base(title)
        {

        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
            {
                Edited = false;
                Running = false;
                return true;
            }
            if (keyEvent.Key == Key.Enter)
            {
                Edited = true;
                Running = false;
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}
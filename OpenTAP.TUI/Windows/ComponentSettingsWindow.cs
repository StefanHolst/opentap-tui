using System.Reflection;
using OpenTap.Tui.Views;
using Terminal.Gui;


namespace OpenTap.Tui.Windows
{
    public class ComponentSettingsWindow : Window
    {
        private ComponentSettings setting { get; set; }

        public ComponentSettingsWindow(ComponentSettings setting) : base(setting.GetType().GetCustomAttribute<DisplayAttribute>()?.Name ?? setting.GetType().Name)
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
                Application.RequestStop();
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}
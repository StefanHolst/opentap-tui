using System.Reflection;
using OpenTap.Tui.Views;
using Terminal.Gui;


namespace OpenTap.Tui.Windows
{
    public class ComponentSettingsWindow : BaseWindow
    {
        private ComponentSettings setting { get; set; }

        public ComponentSettingsWindow(ComponentSettings setting) : base(setting.GetType().GetCustomAttribute<DisplayAttribute>()?.Name ?? setting.GetType().Name)
        {
            Modal = true;
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
                MainWindow.ContainsUnsavedChanges = false;
                Application.RequestStop();
                
                // If changes were made to the tui's own settings we should reload the settings
                if (setting is TuiSettings tuiSettings)
                    tuiSettings.LoadSettings();
                
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}
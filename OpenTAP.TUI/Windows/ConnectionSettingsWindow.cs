using OpenTap.Tui.PropEditProviders;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class ConnectionSettingsWindow : Window
    {
        AnnotationCollection annotation;
        public ConnectionSettingsWindow(string title) : base(title)
        {
            Modal = true;
            
            var Resources = ConnectionSettings.Current;
            if (Resources is ConnectionSettings)
            {
                var prov = new DataGridEditProvider();
                annotation = AnnotationCollection.Annotate(Resources);
                
                var settingsView = prov.Edit(annotation);
                if (settingsView != null)
                    Add(settingsView);
            }
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
            {
                annotation?.Write();
                ComponentSettings.SaveAllCurrentSettings();
                Application.RequestStop();
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}
using OpenTap;
using OpenTAP.TUI.PropEditProviders;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class ConnectionSettingsWindow : Window
    {
        AnnotationCollection annotation;
        public ConnectionSettingsWindow(string title) : base(title)
        {
            var Resources = ConnectionSettings.Current;
            if (Resources is ConnectionSettings)
            {
                var prov = new DataGridEditProvider();
                annotation = AnnotationCollection.Annotate(Resources);
                
                var settingsView = prov.Edit(annotation);
                if (settingsView != null)
                    Add(settingsView);
            }

            X = 0;
            Y = 0;
            Width = Dim.Fill();
            Height = Dim.Fill();
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
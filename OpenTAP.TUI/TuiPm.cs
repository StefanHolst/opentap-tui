using System.Threading;
using OpenTap.Tui.Views;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui
{
    [Display("tui-pm", "Open package manager using TUI.")]
    public class TuiPm : TuiAction
    {
        public override int TuiExecute(CancellationToken cancellationToken)
        {
            new LogPanelView(); // Just to subscribe to log as soon as possible
            
            // Add pm window
            var win = new PackageManagerWindow()
            {
                X = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            
            // Run application
            Application.Run(win);

            return 0;
        }
    }
}
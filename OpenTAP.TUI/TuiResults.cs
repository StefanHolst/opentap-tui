using System.Threading;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui
{
    [Display("tui-results", "Open TUI results viewer.")]
    public class TuiResults : TuiAction
    {
        public override int TuiExecute(CancellationToken cancellationToken)
        {
            var win = new ResultsViewerWindow()
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            
            // Run application
            Application.Run(win);

            return 0;
        }
    }
}
using System.Threading;
using OpenTap.Cli;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui
{
    [Display("tui-pm")]
    public class TuiPm : ICliAction
    {
        public int Execute(CancellationToken cancellationToken)
        {
            // TuiHelpers.CancellationToken = cancellationToken;
            cancellationToken.Register(() =>
            {
                Application.RequestStop();
            });
            
            Application.Init();
            
            var top = Application.Top;
            TUI.SetColorScheme();
            
            // Add settings menu
            var menu = new MenuBar(new []
            {
                new MenuBarItem("Settings", new []
                {
                    new MenuItem("Settings", "", () => {})
                })
            });
            top.Add(menu);

            // Add pm window
            top.Add(new PmWindow()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            });
            
            // Run application
            Application.Run();

            return 0;
        }
    }
}
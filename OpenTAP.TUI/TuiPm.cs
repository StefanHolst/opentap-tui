using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenTap.Cli;
using OpenTap.Package;
using OpenTap.Tui.Views;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui
{
    [Display("tui-pm")]
    public class TuiPm : ICliAction
    {
        public static TraceSource log = Log.CreateSource("TUI");
        public static CancellationToken CancellationToken;
        public static List<HttpPackageRepository> Repositories = new List<HttpPackageRepository>();

        public int Execute(CancellationToken cancellationToken)
        {
            new LogPanelView(); // Just to subscribe to log as soon as possible
            CancellationToken = cancellationToken;
            cancellationToken.Register(() =>
            {
                Application.RequestStop();
            });

            foreach (var repository in PackageManagerSettings.Current.Repositories)
            {
                if (repository.Manager is HttpPackageRepository httprepo)
                    Repositories.Add(httprepo);
                else
                    log.Warning("Only http repositories are supported.");
            }

            try
            {
                // Remove console listener to stop any log messages being printed on top of the TUI
                var consoleListener = OpenTap.Log.GetListeners().OfType<ConsoleTraceListener>().FirstOrDefault();
                if (consoleListener != null)
                    OpenTap.Log.RemoveListener(consoleListener);

                // Stop OpenTAP from taking over the terminal for user inputs.
                UserInput.SetInterface(null);
                
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
                top.Add(new PackageManagerWindow()
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                });
                
                // Run application
                Application.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 0;
        }
    }
}
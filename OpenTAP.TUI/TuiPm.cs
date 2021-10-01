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
    public class TuiPm : TuiAction
    {
        public override int TuiExecute(CancellationToken cancellationToken)
        {
            new LogPanelView(); // Just to subscribe to log as soon as possible
            
            // Add settings menu
            var setting = TypeData.FromType(typeof(PackageManagerSettings));
            var obj = ComponentSettings.GetCurrent(setting.Load());
            var name = setting.GetDisplayAttribute().Name;
            var menu = new MenuBar(new []
            {
                new MenuBarItem("Settings", new []
                {
                    new MenuItem("Settings", name, () =>
                    {
                        var settingsView = new ComponentSettingsWindow(obj);
                        Application.Run(settingsView);
                    })
                })
            });
            Top.Add(menu);

            // Add pm window
            Top.Add(new PackageManagerWindow()
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
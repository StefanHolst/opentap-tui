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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using OpenTap.Cli;
using OpenTap.Package;
using OpenTap.Tui.Views;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui
{
    [Display("tui-results")]
    public class TuiResults : TuiAction
    {
        public override int TuiExecute(CancellationToken cancellationToken)
        {
            var win = new ResultsViewerWindow("Results Viewer")
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            Top.Add(win);
            
            // Run application
            Application.Run();

            return 0;
        }
    }
}
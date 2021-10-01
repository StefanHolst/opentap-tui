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
        private ResultsLoadView resultsLoadView;
        
        public override int TuiExecute(CancellationToken cancellationToken)
        {
            
            var win = new Window("Results Viewer")
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            Top.Add(win);

            // Add results load view
            resultsLoadView = new ResultsLoadView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(75),
                Height = Dim.Fill(1),
            };
            win.Add(resultsLoadView);
            
            // // Add plot view
            // var plotView = new PlotView()
            // {
            //     X = 0,
            //     Y = 0,
            //     Width = Dim.Percent(75),
            //     Height = Dim.Fill(1),
            // };
            // plotView.Plot(plot);
            // win.Add(plotView);

            // Add props view
            var propsView = new PropertiesView()
            {
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var settingsFrame = new FrameView("Chart Settings")
            {
                X = Pos.Right(resultsLoadView),
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            settingsFrame.Add(propsView);
            win.Add(settingsFrame);
            
            // Add helper buttons
            var helperButtons = new HelperButtons
            {
                Width = Dim.Fill(),
                Height = 1,
                Y = Pos.Bottom(resultsLoadView)
            };
            win.Add(helperButtons);
            
            // Run application
            Application.Run();

            return 0;
        }
    }
}
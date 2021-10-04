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
            var win = new ResultViewerWindow("Results Viewer")
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

    public class ResultViewerWindow : EditWindow
    {
        private ResultsLoadView resultsLoadView;
        private PropertiesView propsView;

        public ResultViewerWindow(string title) : base(title)
        {
            // Add results load view
            resultsLoadView = new ResultsLoadView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(75),
                Height = Dim.Fill(1)
            };
            Add(resultsLoadView);
            resultsLoadView.SelectedItemChanged += SelectionChanged;

            // Add props view
            propsView = new PropertiesView()
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                DisableHelperButtons = true
            };
            var settingsFrame = new FrameView("Chart Settings")
            {
                X = Pos.Right(resultsLoadView),
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            settingsFrame.Add(propsView);
            Add(settingsFrame);
            
            // Add helper buttons
            var helperButtons = new HelperButtons
            {
                Width = Dim.Fill(),
                Height = 1,
                Y = Pos.Bottom(resultsLoadView)
            };
            Add(helperButtons);
            
            // Add actions
            var actions = new List<MenuItem>();
            var runAction = new MenuItem("Plot Results", "", PlotResults);
            actions.Add(runAction);
            resultsLoadView.ItemMarkedChanged += (args =>
            {
                if (resultsLoadView.GetMarkedItems().Any())
                    HelperButtons.SetActions(actions);
                else
                {
                    HelperButtons.SetActions(new List<MenuItem>());
                }
                
                Application.Refresh();
            });
        }
        
        void SelectionChanged(ListViewItemEventArgs args)
        {
            if (args.Value is ResultsLoadView.IDataViewModel dvm)
            {
                propsView.LoadProperties(dvm.Settings);
            }
        }

        void PlotResults()
        {
            // if (Application.Current is EditWindow)
            //     return;
            
            // Add plot view
            var plotView = new PlotView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            
            var markedItems = resultsLoadView.GetMarkedItems();
            for (var i = 0; i < markedItems.Count; i++)
            {
                var plots = markedItems[i].Settings.PlotCharts();
                foreach (var plot in plots)
                    plotView.Plot(plot);
            }
            
            var dialog = new EditWindow(markedItems.FirstOrDefault()?.Data.Name ?? "Chart")
            {
                Width = Dim.Fill(2),
                Height = Dim.Fill(2),
                X = Pos.Center(),
                Y = Pos.Center(),
                ColorScheme = Colors.Dialog
            };
            dialog.Add(plotView);

            Application.Run(dialog);
        }
        
        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (TuiAction.CurrentAction is TuiResults && (keyEvent.Key == Key.ControlX || keyEvent.Key == Key.ControlC || keyEvent.Key == Key.Esc))
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                {
                    Application.Shutdown();
                }
            }
            
            HelperButtons.Instance?.ProcessKey(keyEvent);
            return base.ProcessKey(keyEvent);
        }

    }
}
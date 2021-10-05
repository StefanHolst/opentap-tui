using System;
using System.Collections.Generic;
using System.Linq;
using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class ResultsViewerWindow : EditWindow
    {
        private RunExplorerView resultsLoadView;
        private PropertiesView propsView;

        public ResultsViewerWindow(string title) : base(title)
        {
            // Add results load view
            resultsLoadView = new RunExplorerView()
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
                    HelperButtons.SetActions(actions, this);
                else
                {
                    HelperButtons.SetActions(new List<MenuItem>(), this);
                }
                
                Application.Refresh();
            });
        }
        
        void SelectionChanged(ListViewItemEventArgs args)
        {
            if (args.Value is RunExplorerView.IDataViewModel dvm)
            {
                propsView.LoadProperties(dvm.Settings);
            }
        }

        void PlotResults()
        {
            if (TuiAction.CurrentAction is TUI && IsCurrentTop == false || TuiAction.CurrentAction is TuiResults && Application.Current is EditWindow)
                return;
            
            // Add plot view
            var plotView = new PlotView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            
            var markedItems = resultsLoadView.GetMarkedItems();
            foreach (var item in markedItems)
            {
                var plots = item.Settings.PlotCharts();
                foreach (var plot in plots)
                    plotView.Plot(plot);
            }
            
            var dialog = new EditWindow((markedItems.FirstOrDefault()?.Data.Name ?? "Chart") + " - " + String.Join("  ", plotView.Legends))
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

                return true;
            }

            if (HelperButtons.Instance?.ProcessKey(keyEvent) == true)
                return true;
            
            return base.ProcessKey(keyEvent);
        }
    }
}
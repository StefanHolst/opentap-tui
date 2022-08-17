using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenTap.Plugins.BasicSteps;
using OpenTap.Tui.Views;
using Terminal.Gui;
using Terminal.Gui.Graphs;

namespace OpenTap.Tui.Windows
{
    public class ResultsWindow : Window
    {
        private PropertiesView propsView;
        private GraphView graphView;
        private HelperButtons helperButtons;
        private ChartSettings settings;

        public ResultsWindow(List<IRunViewModel> runs)
        {
            Modal = true;
            settings = new ChartSettings(runs);
            settings.PropertyChanged += PlotResults;

            // Add plot
            graphView = new GraphView()
            {
                Width = Dim.Percent(75),
                Height = Dim.Fill(1)
            };
            Add(graphView);
            
            // Add props view
            var settingsFrame = new FrameView("Plot Settings")
            {
                X = Pos.Right(graphView),
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            propsView = new PropertiesView()
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                DisableHelperButtons = true
            };
            propsView.LoadProperties(settings);
            settingsFrame.Add(propsView);
            Add(settingsFrame);
            
            // Add helper buttons
            helperButtons = new HelperButtons()
            {
                Y = Pos.Bottom(graphView)
            };
            Add(helperButtons);
            
            // Add export action
            var actions = new List<MenuItem>();
            var exporters = TypeData.GetDerivedTypes<ITableExport>().ToList();
            var exportAction = new MenuItem("Export", "", () => Export(exporters), shortcut: Key.F5);
            exportAction.CanExecute += () => exporters.Any();
            actions.Add(exportAction);

            var zoomAction = new MenuItem("Zoom", "", () => Zoom((float)0.8), shortcut: Key.F6);
            actions.Add(zoomAction);
            var zoomOutAction = new MenuItem("Zoom", "", () => Zoom((float)1.25), shortcut: Key.F7);
            actions.Add(zoomOutAction);
            
            helperButtons.SetActions(actions, this);
            helperButtons.CanFocus = false;

            Loaded += PlotResults;
        }

        void Zoom(float factor)
        {
            graphView.CellSize = new PointF (
                graphView.CellSize.X * factor,
                graphView.CellSize.Y * factor
            );

            graphView.AxisX.Increment *= factor;
            graphView.AxisY.Increment *= factor;

            graphView.SetNeedsDisplay ();
        }

        void PlotResults()
        {
            graphView.Reset ();
            
            var plots = settings.GetPlots();

            var series = plots.Select(p => p.Series).Where(s => s != null).ToList();
            var plotAnnotations = plots.Select(p => p.PathAnnotation).Where(a => a != null).ToList();
            
            if (series.Any())
                graphView.Series.AddRange(series);
            if (plotAnnotations.Any())
                graphView.Annotations.AddRange(plotAnnotations);

            graphView.AxisX.Text = settings.XAxis?.Name;
            graphView.AxisY.Text = settings.YAxis?.Name;

            // Set offset in order to fit the data in view
            var points = plots.SelectMany(p => p.Points).ToList();
            if (points.Any())
            {
                var maxX = points.Max(p => p.X);
                var maxY = points.Max(p => p.Y);
                var width = graphView.Bounds.Width - 2;
                var height = graphView.Bounds.Height - 2;
                var factor = Math.Max(maxX / width, maxY / height);
                graphView.CellSize = new PointF (
                    factor,
                    factor
                );
                graphView.AxisX.Increment = factor;
                graphView.AxisY.Increment = factor;
                graphView.MarginLeft = 1;
                graphView.MarginBottom = 1;
            }
            
            // Add legends
            var legend = new LegendAnnotation (new Rect (graphView.Bounds.Width - 20,0, 20, plots.Count + 2));
            foreach (var plot in plots)
                legend.AddEntry (new GraphCellToRender (plot.Fill.Rune), plot.Name);
            graphView.Annotations.Add (legend);
            
            Title = string.Join(", ", settings.ResultNames);
        }
        
        void Export(List<ITypeData> exporters)
        {
            var request = new ExportDialogInput();
            request.AvailableExporters = exporters.Select(e => e.CreateInstance() as ITableExport).ToList();
            if (request.AvailableExporters.Count == 1)
                request.Exporter = request.AvailableExporters.FirstOrDefault();
            UserInput.Request(request);
        
            if (request.Submit == ExportDialogInput.ExportSubmit.Ok && request.Exporter != null && request.Path != null)
            {
                var plots = settings.GetPlots();
                var maxLength = plots.Max(p => p.Points.Count);
                var exportingResult = new string[maxLength + 1][];
                
                // Add headers
                var headers = new List<string>();
                for (int i = 0; i < plots.Count; i++)
                {
                    var plot = plots[i];
                    headers.Add(plot.XAxisTitle);
                    headers.Add(plot.YAxisTitle);
                }
                exportingResult[0] = headers.ToArray();
                
                // Add values
                for (int i = 1; i <= maxLength; i++)
                {
                    var values = new List<string>();
                    for (int j = 0; j < plots.Count; j++)
                    {
                        var plot = plots[j];
                        if (plot.Points.Count <= i)
                            break;
                        values.Add(plot.Points[i].X.ToString(NumberFormatInfo.CurrentInfo));
                        values.Add(plot.Points[i].Y.ToString(NumberFormatInfo.CurrentInfo));
                    }
                    exportingResult[i] = values.ToArray();
                }
                
                
                request.Exporter.ExportTableValues(exportingResult, request.Path);
            }
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
            {
                var handled = base.ProcessKey(keyEvent);
                if (handled) return true;
                Application.RequestStop();
                return true;
            }

            if (helperButtons.ProcessKey(keyEvent) == true)
                return true;
            
            return base.ProcessKey(keyEvent);
        }
    }
}
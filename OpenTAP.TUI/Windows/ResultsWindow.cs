using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using OpenTap.Plugins.BasicSteps;
using OpenTap.Tui.Annotations;
using OpenTap.Tui.Views;
using Terminal.Gui;
using Terminal.Gui.Graphs;

namespace OpenTap.Tui.Windows
{
    public class ResultsWindow : Window
    {
        private PropertiesView propsView;
        private FrameView plotFrame;
        private GraphView graphView;
        // private PlotView plotView;
        private HelperButtons helperButtons;
        private List<IRunViewModel> runs;
        private int selectedIndex = 0;
        private ChartSettings Settings;

        public ResultsWindow(List<IRunViewModel> runs)
        {
            Modal = true;
            Settings = new ChartSettings(runs);
            Settings.PropertyChanged += PlotResults;

            // Add plot
            plotFrame = new FrameView()
            {
                Width = Dim.Percent(75),
                Height = Dim.Fill(1)
            };
            graphView = new GraphView()
            {
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            // plotView = new PlotView(Settings)
            // {
            //     Width = Dim.Fill(),
            //     Height = Dim.Fill()
            // };
            plotFrame.Add(graphView);
            Add(plotFrame);
            
            // Add props view
            var settingsFrame = new FrameView("Plot Settings")
            {
                X = Pos.Right(plotFrame),
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            propsView = new PropertiesView()
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                DisableHelperButtons = true
            };
            propsView.LoadProperties(Settings);
            settingsFrame.Add(propsView);
            Add(settingsFrame);
            
            // Add helper buttons
            helperButtons = new HelperButtons()
            {
                Y = Pos.Bottom(plotFrame)
            };
            Add(helperButtons);
            
            // Add export action
            var actions = new List<MenuItem>();
            var exporters = TypeData.GetDerivedTypes<ITableExport>().ToList();
            if (exporters.Any())
            {
                var exportAction = new MenuItem("Export", "", () => Export(exporters));
                actions.Add(exportAction);
            }
            helperButtons.SetActions(actions, this);

            PlotResults();
        }

        void PlotResults()
        {
            graphView.Reset ();

            
            var plots = Settings.GetPlots();
            foreach (var plot in plots)
            {
                var scatterSeries = new ScatterSeries();
                scatterSeries.Points = plot.PointFs;
                graphView.Series.Add(scatterSeries);
            }
            
                
                
                
                
            //     
            // plotView.Reset();
            // if (Settings.ResultNames.Any() == false)
            //     return;
            //
            // var plots = Settings.GetPlots();
            // foreach (var plot in plots)
            //     plotView.Plot(plot);
            //
            // plotFrame.Title = string.Join("  ", plotView.Legends);
            Title = string.Join(", ", Settings.ResultNames);
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
                var plots = Settings.GetPlots();
                var maxLength = plots.Max(p => p.Points.Values.Count);
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
                        if (plot.Points.Keys.Count <= i)
                            break;
                        values.Add(plot.Points.Keys.ElementAt(i).ToString(NumberFormatInfo.CurrentInfo));
                        values.Add(plot.Points.Values.ElementAt(i).ToString(NumberFormatInfo.CurrentInfo));
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
    
    public class ChartSettings
    {
        public Action PropertyChanged;
        private Dictionary<string, List<IResultTable>> allSeries;
        public ChartSettings(List<IRunViewModel> runs)
        {
            allSeries = runs.SelectMany(r => r.Series) // Join series from all runs
                .GroupBy(s => s.Key) // Group all series by their name
                .ToDictionary(g => g.Key, v => v.SelectMany(s => s.Value).ToList()); // Create a dictionary with name as key and series as value

            foreach (var series in allSeries)
                ResultNames.Add(series.Key);
            foreach (var availableStepName in AvailableStepNames)
                FilterStepName.Add(availableStepName);
            
            bool updatingResults = false;
            FilterStepName.CollectionChanged += (sender, args) =>
            {
                if (updatingResults == false)
                    PropertyChanged?.Invoke();
            };
            ResultNames.CollectionChanged += (sender, args) =>
            {
                updatingResults = true;
                
                FilterStepName.Clear();
                foreach (var availableStepName in AvailableStepNames)
                    FilterStepName.Add(availableStepName);
                
                updatingResults = false;
                PropertyChanged?.Invoke();
            };
        }

        public List<string> AvailableResults => allSeries.Select(s => s.Key).ToList();

        [AvailableValues(nameof(AvailableResults))]
        [Display("Result Names")]
        public ObservableCollection<string> ResultNames { get; set; } = new ObservableCollection<string>();
        
        public List<string> AvailableStepNames => allSeries.Where(s => ResultNames.Contains(s.Key)).SelectMany(s => s.Value).Select(s => s.Parent.Name).Distinct().ToList();

        [AvailableValues(nameof(AvailableStepNames))]
        [Display("Step Name", Group: "Filter")]
        public ObservableCollection<string> FilterStepName { get; set; } = new ObservableCollection<string>();
        
        public List<IResultColumn> AvailableResultColumns
        {
            get
            {
                var list = new List<IResultColumn>();
                var results = allSeries.Where(s => ResultNames.Contains(s.Key)).SelectMany(s => s.Value).Where(s => FilterStepName.Contains(s.Parent.Name)).ToList();
                foreach (var result in results)
                {
                    if (list.Any() == false)
                        list.Add(new ResultColumn("[Index]", Enumerable.Range(0, result.Columns.FirstOrDefault()?.Data.Length ?? 0).ToArray()));
                    
                    foreach (var column in result.Columns)
                        if (list.Any(r => r.Name == column.Name) == false)
                            list.Add((column));
                }
                return list;
            }
        }
        
        private IResultColumn xAxis;
        [Display("X-Axis", Group: "Plot Data")]
        [AvailableValues(nameof(AvailableResultColumns))]
        public IResultColumn XAxis
        {
            get
            {
                if (xAxis == null || AvailableResultColumns.Any(c => c.Name == xAxis.Name && IResultColumnAnnotation.CanConvertToDouble(c)) == false)
                    xAxis = AvailableResultColumns.FirstOrDefault(IResultColumnAnnotation.CanConvertToDouble);
                return xAxis;
            }
            set
            {
                xAxis = value;
                PropertyChanged?.Invoke();
            }
        }

        private IResultColumn yAxis;
        [Display("Y-Axis", Group: "Plot Data")]
        [AvailableValues(nameof(AvailableResultColumns))]
        public IResultColumn YAxis
        {
            get
            {
                if (yAxis == null || AvailableResultColumns.Any(c => c.Name == xAxis.Name && IResultColumnAnnotation.CanConvertToDouble(c)) == false)
                    yAxis = AvailableResultColumns.FirstOrDefault(c => IResultColumnAnnotation.CanConvertToDouble(c) && c.Name != xAxis.Name);
                return yAxis;
            }
            set
            {
                yAxis = value;
                PropertyChanged?.Invoke();
            }
        }

        private bool logXAxis;
        [Display("Log X-Axis", Group: "Plot Data")]
        public bool LogXAxis
        {
            get => logXAxis;
            set
            {
                logXAxis = value;
                PropertyChanged?.Invoke();
            }
        }
        private bool logYAxis;
        [Display("Log Y-Axis", Group: "Plot Data")]
        public bool LogYAxis
        {
            get => logYAxis;
            set
            {
                logYAxis = value;
                PropertyChanged?.Invoke();
            }
        }

        
        public List<Plot> GetPlots()
        {
            var list = new List<Plot>();
            var results = allSeries.Where(s => ResultNames.Contains(s.Key)).SelectMany(s => s.Value).Where(s => FilterStepName.Contains(s.Parent.Name)).ToList();
            foreach (var result in results)
            {
                IResultColumn xaxis;
                if (XAxis.Name == "[Index]")
                    xaxis = new ResultColumn("[Index]", Enumerable.Range(0, result.Columns.FirstOrDefault()?.Data.Length ?? 0).ToArray());
                else
                    xaxis = result.Columns.FirstOrDefault(c => XAxis.Name == c.Name);
                IResultColumn yaxis;
                if (YAxis.Name == "[Index]")
                    yaxis = new ResultColumn("[Index]", Enumerable.Range(0, result.Columns.FirstOrDefault()?.Data.Length ?? 0).ToArray());
                else
                    yaxis = result.Columns.FirstOrDefault(c => YAxis.Name == c.Name);
                
                if (xaxis == null || yaxis == null)
                    continue;
                
                try
                {
                    var plot = new Plot(result.Parent?.Name ?? result.Name, xaxis.Name, yaxis.Name);
                    for (int i = 0; i < xaxis.Data.Length; i++)
                    {
                        var x = Convert.ToDouble(xaxis.Data.GetValue(i));
                        var y = Convert.ToDouble(yaxis.Data.GetValue(i));
                        plot.Points[x] = y;
                        plot.PointFs.Add(new PointF((float)x, (float)y));
                    }
                    list.Add(plot);
                }
                catch (Exception e)
                {
                    MessageBox.Query(10, 10, "Failed to Plot", "Failed to parse data to plot: " + e.Message);
                }
            }
            return list;
        }
    }
}
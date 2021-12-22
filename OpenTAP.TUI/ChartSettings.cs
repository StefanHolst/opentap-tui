using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenTap.Tui.Annotations;
using OpenTap.Tui.Views;
using OpenTap.Tui.Windows;
using Terminal.Gui;
using Terminal.Gui.Graphs;

namespace OpenTap.Tui
{
    public enum ChartType
    {
        [Display("XY Scatter")]
        Scatter,
        [Display("XY Line")]
        Line,
        [Display("Column")]
        Column
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

        private ChartType chartType;
        [Display("Chart Type")]
        public ChartType ChartType
        {
            get => chartType;
            set
            {
                chartType = value;
                PropertyChanged?.Invoke();
            }
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

        private char[] Symbols = {  (char)Application.Driver.Selected, (char)Application.Driver.UnSelected, (char)Application.Driver.Diamond, (char)Application.Driver.Lozenge, '#', '+', '*' };
        
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
                    var points = new List<PointF>();
                    for (int i = 0; i < xaxis.Data.Length; i++)
                    {
                        var x = Convert.ToDouble(xaxis.Data.GetValue(i));
                        var y = Convert.ToDouble(yaxis.Data.GetValue(i));
                        points.Add(new PointF((float)x, (float)y));
                    }

                    if (logXAxis || logYAxis)
                    {
                        var minX = Math.Max(points.Min(p => p.X), 0.1);
                        var maxX = points.Max(p => p.X);
                        var minY = Math.Max(points.Min(p => p.Y), 0.1);
                        var maxY = points.Max(p => p.Y);
                        
                        for (int i = 0; i < points.Count; i++)
                        {
                            var point = points[i];
                            points[i] = new PointF(
                                (float) (logXAxis ? ScaleHelpers.ScaleLog(point.X, minX, maxX) : point.X), 
                                (float) (logYAxis ? ScaleHelpers.ScaleLog(point.Y, minY, maxY) : point.Y)
                                );
                        }
                    }
                    
                    var plot = new Plot(result.Parent?.Name ?? result.Name, xaxis.Name, yaxis.Name);
                    
                    plot.Points = points;
                    plot.Fill = new GraphCellToRender(Symbols[(list.Count) % Symbols.Length]);
                    if (ChartType == ChartType.Scatter || ChartType == ChartType.Line)
                    {
                        plot.Fill = new GraphCellToRender(Symbols[(list.Count) % Symbols.Length]);
                        plot.Series = new ScatterSeries()
                        {
                            Points = points,
                            Fill = new GraphCellToRender(Symbols[(list.Count) % Symbols.Length])
                        };

                        if (ChartType == ChartType.Line)
                        {
                            plot.PathAnnotation = new PathAnnotation()
                            {
                                Points = points,
                                BeforeSeries = true
                            };
                        }
                    }

                    if (chartType == ChartType.Column)
                    {
                        var Stipple = new GraphCellToRender (Application.Driver.Stipple);
                        plot.Series = new BarSeries()
                        {
                            Bars = points.Select(p => new BarSeries.Bar("", Stipple, p.Y)).ToList()
                        };
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
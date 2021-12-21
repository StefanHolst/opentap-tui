using System;
using System.Collections.Generic;
using System.Linq;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class PlotView : View
    {
        private ChartSettings Settings;
        private List<Plot> Plots { get; set; } = new List<Plot>();
        private char[] Symbols = {  (char)Driver.Selected, (char)Driver.UnSelected, (char)Driver.Diamond, (char)Driver.Lozenge, '#', '+', '*' };
        public List<string> Legends = new List<string>();

        public PlotView(ChartSettings settings)
        {
            Settings = settings;
        }

        public void Reset()
        {
            Clear();
            Plots = new List<Plot>();
            Legends = new List<string>();
        }
        
        public void Plot(Plot plot)
        {
            Plots.Add(plot);
            Legends.Add($"{Symbols[(Plots.Count-1) % Symbols.Length]} {plot.Name}");
        }
        
        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            if (Plots.Any() == false)
                return;
            
            Driver.SetAttribute(ColorScheme.Normal);
            
            // Find max values for scaling
            var allXValues = Plots.SelectMany(p => p.Points.Keys).ToList();
            var allYValues = Plots.SelectMany(p => p.Points.Values).ToList();
            var plotSize = new PlotSize(allXValues.Min(), allYValues.Min(), allXValues.Max(), allYValues.Max());
            
            // Set X and Y axis values
            var YWidth = Math.Max(((int)plotSize.MaxY).ToString().Length, ((int)plotSize.MinY).ToString().Length);
            // Size of box
            var size = new Rect(1 + YWidth, 0, Frame.Width - 1, Frame.Height - 3);

            DrawYAxis(YWidth, size, plotSize);
            DrawXAxis(size, plotSize);
            PrintAxisNames();
            
            for (int i = 0; i < Plots.Count; i++)
            {
                var scaledPlot = new ScaledPlot(Plots[i], size, plotSize, Settings);
                DrawPlot(scaledPlot, Symbols[i % Symbols.Length], size);
            }
        }

        void PrintAxisNames()
        {
            var plot = Plots.FirstOrDefault();
            if (plot == null)
                return;
            
            // Print x axis title
            var xAxis = plot.XAxisTitle;
            var xOffset = (Frame.Width / 4) * 3 - xAxis.Length;
            Move(xOffset, Frame.Height - 1);
            Driver.AddRune(Driver.HLine);
            Driver.AddStr($" {xAxis}");
            
            // Print y axis title
            var yAxis = plot.YAxisTitle;
            var yOffset = Frame.Width / 4 - yAxis.Length;
            Move(yOffset, Frame.Height - 1);
            Driver.AddRune(Driver.VLine);
            Driver.AddStr($" {yAxis}");
        }

        private const int YLINECOUNT = 5;
        void DrawYAxis(int axisWidth, Rect size, PlotSize plotSize)
        {
            var difference = plotSize.MaxY - plotSize.MinY;
            var interval = difference / size.Height;

            for (var i = 0; i < size.Height; i++)
            {
                if (i % YLINECOUNT == 0) // Every 5 lines print y value
                {
                    var value = Math.Round(plotSize.MaxY - i * interval);
                    
                    if (Settings.LogYAxis)
                        value = ScaleHelpers.ScaleLog(value, Math.Max(plotSize.MinY, 1), plotSize.MaxY);
                    
                    var num = string.Format("{0," + axisWidth + ":0}", value);
                    
                    for (int j = 0; j < num.Length; j++)
                    {
                        Move(j, i);
                        Driver.AddRune(num[j]);
                    }
                    Move(num.Length, i);
                    Driver.AddRune(Driver.RightTee);
                }
                else
                {
                    Move(axisWidth, i);
                    Driver.AddRune(Driver.VLine);
                }
            }
        }

        private const int XLINECOUNT = 15;
        void DrawXAxis(Rect size, PlotSize plotSize)
        {
            var difference = plotSize.MaxX - plotSize.MinX;
            var interval = difference / size.Width;

            for (var i = size.X + 1; i < size.Width; i++)
            {
                if (i % XLINECOUNT == 0) // Every 15 lines print x value
                {
                    var value = Math.Round(plotSize.MinX + i * interval);
                    
                    if (Settings.LogXAxis)
                        value = ScaleHelpers.ScaleLog(value, Math.Max(plotSize.MinX, 1), plotSize.MaxX);

                    var num = value.ToString("0");
                    for (var j = 0; j < num.Length; j++)
                    {
                        Move(i + j - num.Length/2, size.Height + 1);
                        Driver.AddRune(num[j]);
                    }
                    Move(i, size.Height);
                    Driver.AddRune(Driver.TopTee);
                }
                else
                {
                    Move(i, size.Height);
                    Driver.AddRune(Driver.HLine);
                }
            }
        }

        void DrawPlot(ScaledPlot plot, char symbol, Rect size)
        {
            for (int y = 0; y < size.Height; y++)
            {
                for (int x = 0; x < size.Width; x++) 
                {
                    var material = plot.IsPoint(x, y);
                    if (material)
                    {
                        Move(x, y);

                        var rune = new Rune(symbol);
                        Driver.AddRune(rune);
                    }
                }
            }
        }
    }

    public class Plot
    {
        public string Name { get; set; }
        public string XAxisTitle { get; set; }
        public string YAxisTitle { get; set; }
        public Dictionary<double, double> Points { get; set; }
        public List<PointF> PointFs { get; set; }
        
        public Plot(string name, string xAxisTitle, string yAxisTitle)
        {
            Name = name;
            XAxisTitle = xAxisTitle;
            YAxisTitle = yAxisTitle;
            Points = new Dictionary<double, double>();
            PointFs = new List<PointF>();
        }
    }

    public class ScaledPlot
    {
        private Dictionary<int, int> Points { get; set; }

        public bool IsPoint(int x, int y)
        {
            return Points.Contains(new KeyValuePair<int, int>(x, y));
        }
        
        public ScaledPlot(Plot plot, Rect size, PlotSize plotSize, ChartSettings settings)
        {
            Points = new Dictionary<int, int>();
            
            foreach (var point in plot.Points)
            {
                var xValue = settings.LogXAxis ? ScaleHelpers.ScaleExp(point.Key, Math.Max(plotSize.MinX, 1), plotSize.MaxX) : point.Key;
                var x = (int) Math.Round(size.X + (size.Width - size.X - 1) * ((xValue - plotSize.MinX) / (plotSize.MaxX - plotSize.MinX)));
                
                var yValue = settings.LogYAxis ? ScaleHelpers.ScaleExp(point.Value, Math.Max(plotSize.MinY, 1), plotSize.MaxY) : point.Value;
                var value = plotSize.MaxY - yValue + plotSize.MinY; // Because the y axis starts from the top (max value) we invert it here
                var y = (int) Math.Round(size.Y + (size.Height - size.Y - 1) * ((value - plotSize.MinY) / (plotSize.MaxY - plotSize.MinY)));
                Points[x] = y;
            }
        }
    }

    public class PlotSize
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        
        public PlotSize(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
    }

    public class ScaleHelpers
    {
        // https://stackoverflow.com/a/63158920
        
        //  x - x0    log(y) - log(y0)
        // ------- = -----------------
        // x1 - x0   log(y1) - log(y0)
        
        
        public static double ScaleLog(double value, double min, double max, int log = 10)
        {
            //         /  x - x0                                \
            // y = 10^|  ------- * (log(y1) - log(y0)) + log(y0) |
            //         \ x1 - x0                                /
            
            return Math.Pow(log, ((value - min) / (max - min)) * (Math.Log(max, log) - Math.Log(min, log)) + Math.Log(min, log));
        }
        public static double ScaleExp(double value, double min, double max, int log = 10)
        {
            //                  log(y) - log(y0)
            // x = (x1 - x0) * ----------------- + x0
            //                 log(y1) - log(y0)

            return (max - min) * ((Math.Log(value, log) - Math.Log(min, log)) / (Math.Log(max, log) - Math.Log(min, log))) + min;
        }
    }
}
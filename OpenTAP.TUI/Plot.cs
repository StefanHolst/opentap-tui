using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Terminal.Gui.Graphs;

namespace OpenTap.Tui
{
    public class Plot
    {
        public string Name { get; set; }
        public string XAxisTitle { get; set; }
        public string YAxisTitle { get; set; }
        public GraphCellToRender Fill { get; set; }
        
        public List<PointF> Points { get; set; }
        public LegendAnnotation Legend { get; set; }

        public ISeries Series { get; set; }

        public ScatterSeries GetSeries(ChartSettings settings)
        {
            var series = new ScatterSeries();
            var minX = Points.Select(p => p.X).Min();
            var maxX = Points.Select(p => p.X).Max();
            var minY = Points.Select(p => p.Y).Min();
            var maxY = Points.Select(p => p.Y).Max();
            
            foreach (var point in Points)
            {
                float x = (float) (settings.LogXAxis ? ScaleHelpers.ScaleLog(point.X, minX, maxX) : point.X);
                float y = (float) (settings.LogYAxis ? ScaleHelpers.ScaleLog(point.Y, minY, maxY) : point.Y);
                series.Points.Add(new PointF(x, y));
            }

            if (Fill != null)
                series.Fill = Fill;

            return series;
        }
        
        public PathAnnotation PathAnnotation { get; set; }

        public Plot(string name, string xAxisTitle, string yAxisTitle)
        {
            Name = name;
            XAxisTitle = xAxisTitle;
            YAxisTitle = yAxisTitle;
        }
    }

    public class PointD
    {
        public double X { get; set; }
        public double Y { get; set; }

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }
    }


    public static class ScaleHelpers
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
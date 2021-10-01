using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public class PlotView : FrameView
    {
        private List<Plot> Plots { get; set; } = new List<Plot>();

        public void Plot(Plot plot)
        {
            Plots.Add(plot);
        }
        
        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            Driver.SetAttribute(ColorScheme.Normal);

            foreach (var plot in Plots)
            {
                var scaledPlot = new ScaledPlot(plot, new Rect(1,1,Frame.Width - 2, Frame.Height - 2));
                DrawPlot(scaledPlot);
            }
        }

        void DrawPlot(ScaledPlot plot)
        {
            for (int y = 0; y < Frame.Height; y++)
            {
                for (int x = 0; x < Frame.Width; x++) 
                {
                    var material = plot.IsPoint(x, y);
                    if (material)
                    {
                        Move(x, y);

                        var rune = new Rune(plot.Symbol);
                        Driver.AddRune(rune);
                    }
                }
            }
        }
    }

    public class Plot
    {
        public Dictionary<double, double> Points { get; set; }
        public char Symbol { get; set; }
        
        public Plot(Dictionary<double, double> points, char symbol = '*')
        {
            Points = points;
            this.Symbol = symbol;
        }

        public Plot()
        {
            Points = new Dictionary<double, double>();
            Symbol = '*';
        }
    }

    public class ScaledPlot
    {
        public Dictionary<int, int> Points { get; set; }
        public char Symbol { get; set; }
        public Rect Size { get; set; }

        public bool IsPoint(int x, int y)
        {
            return Points.Contains(new KeyValuePair<int, int>(x, y));
        }
        
        public ScaledPlot(Plot plot, Rect size)
        {
            Symbol = plot.Symbol;
            Size = size;
            Points = new Dictionary<int, int>();

            // Scale the points
            var maxX = plot.Points.Keys.Max();
            var minX = plot.Points.Keys.Min();
            var maxY = plot.Points.Values.Max();
            var minY = plot.Points.Values.Min();
            
            foreach (var point in plot.Points)
            {
                var x = (int)(size.X + (size.Width - size.X) * ((point.Key - minX) / (maxX - minX)));
                var y = (int)(size.Y + (size.Height - size.Y) * ((point.Value-minY) / (maxY - minY)));
                Points[x] = y;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class ResultsLoadView : FrameView
    {
        private ListView resultsList;
        private List<string> results = new List<string>();
        private string[] Headers =  { "Run ID", "Name", "Verdict", "Tags" };
        private int HeadersLength = 0;
        private int currentWidth = 0;

        public ResultsLoadView()
        {
            HeadersLength = string.Join("", Headers).Length;
            
            resultsList = new ListView()
            {
                Height = Dim.Fill(),
                Width = Dim.Fill()
            };
            Add(resultsList);
            resultsList.SetSource(results);
            
            // Add actions
            var actions = new List<MenuItem>();
            var runAction = new MenuItem("Load Results", "", LoadEntries);
            actions.Add(runAction);
            Enter += args =>
            {
                HelperButtons.SetActions(actions);
            };
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter && MostFocused == resultsList)
            {
                // Create a plot
                var plot = new Plot();
                for (double x = 0; x < 100; x++)
                    plot.Points[x] = Math.Sin((double)x / 100 * 2 * Math.PI);

                var plotView = new PlotView();
                plotView.Plot(plot);
                
                var win = new EditWindow("Plot");
                win.Add(plotView);
                Application.Run(win);
            }
            
            HelperButtons.Instance?.ProcessKey(keyEvent);
            return base.ProcessKey(keyEvent);
        }

        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            // Only update if size has changed
            if (currentWidth == bounds.Width)
                return;
            currentWidth = bounds.Width;
            
            var columnWidth = (bounds.Width - 4) / Headers.Length;
            string title = "";
            foreach (var header in Headers)
            {
                title += header + new string('-', columnWidth - header.Length);
            }
            Title = title;
            LoadEntries();
        }

        private void LoadEntries()
        {
            results.Clear();
            var stores = ResultSettings.Current.OfType<IResultStore>().ToList();
            var spacing = (Bounds.Width - HeadersLength) / (Headers.Length);
            
            foreach (var store in stores)
            {
                try
                {
                    store.Open();
                    var entries = store.GetEntries(SearchCondition.All(), new List<string>(), false).ToList();

                    foreach (var entry in entries)
                    {
                        var id = entry.GetID().ToString();
                        var name = entry.Name;
                        var verdict = entry.Parameters.FirstOrDefault(p => p.Name == "Verdict")?.Value.ToString();
                        var tags = entry.Parameters.FirstOrDefault(p => p.Name == "Tags")?.Value.ToString();

                        var row = new StringBuilder();
                        row.Append(" ");
                        row.Append(id);
                        row.Append(new string(' ', Headers[0].Length + spacing - id.Length - 1));
                        row.Append(name);
                        row.Append(new string(' ', Headers[1].Length + spacing + 1 - name.Length));
                        row.Append(verdict);
                        row.Append(new string(' ', Headers[1].Length + spacing + 1 - verdict?.Length ?? 0));
                        row.Append(tags);
                        row.Append(new string(' ', Headers[1].Length + spacing - tags?.Length ?? 0));

                        results.Add(row.ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    store.Close();
                }
            }
        }
    }
}
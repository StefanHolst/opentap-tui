using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using OpenTap.Tui.Windows;
using Terminal.Gui;
using OpenTap.Plugins;

namespace OpenTap.Tui.Views
{
    public class ResultsLoadView : View
    {
        public static string[] Headers =  { "Run ID", "Name", "Verdict", "Tags" };
        
        private SelectorView runList;
        public Action<ListViewItemEventArgs> SelectedItemChanged;
        public Action<ListViewItemEventArgs> ItemMarkedChanged;
        public List<IDataViewModel> GetMarkedItems()
        {
            return runList.MarkedItems().Select(i => i as IDataViewModel).ToList();
        }

        public ResultsLoadView()
        {
            var frames = new List<FrameView>();
            for (int i = 0; i < Headers.Length; i++)
            {
                var header = Headers[i];
                var frame = new FrameView(header)
                {
                    Width = Dim.Percent(100 / Headers.Length)
                };
                if (i > 0)
                    frame.X = Pos.Right(frames.LastOrDefault());
                
                frames.Add(frame);
                Add(frame);
            }
            
            runList = new SelectorView
            {
                Height = Dim.Fill(),
                Width = Dim.Fill()
            };
            runList.SelectedItemChanged += args => SelectedItemChanged?.Invoke(args);
            runList.ItemMarkedChanged += args => ItemMarkedChanged?.Invoke(args);
            var resultFrame = new FrameView()
            {
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            resultFrame.Add(runList);
            Add(resultFrame);
            
            LoadEntries();
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            // if (keyEvent.Key == Key.Enter && MostFocused == resultsList)
            // {
            //     // Create a plot
            //     var plot = new Plot();
            //     for (double x = 0; x < 100; x++)
            //         plot.Points[x] = Math.Sin((double)x / 100 * 2 * Math.PI);
            //
            //     var plotView = new PlotView();
            //     plotView.Plot(plot);
            //     
            //     var win = new EditWindow("Plot");
            //     win.Add(plotView);
            //     Application.Run(win);
            // }
            
            HelperButtons.Instance?.ProcessKey(keyEvent);
            return base.ProcessKey(keyEvent);
        }

        private void LoadEntries()
        {
            var list = new List<IDataViewModel>();
            var stores = ResultSettings.Current.OfType<IResultStore>().ToList();

            foreach (var store in stores)
            {
                try
                {
                    store.Open();
                    var allEntries = store.GetEntries(SearchCondition.All(), new List<string>(), true);

                    var runs = new Dictionary<long, IDataViewModel>();

                    foreach (var entry in allEntries)
                    {
                        if (entry.ObjectType == "Plan Run")
                        {
                            var run = new IDataViewModel(entry, this);
                            runs[entry.GetID()] = run;
                            list.Add(run);
                        }
                        else if (entry is IResultTable resultTable)
                        {
                            var parent = entry;
                            while (true)
                            {
                                if (parent.Parent == null)
                                    break;
                                parent = parent.Parent;
                            }
                            
                            var run = runs[parent.GetID()];
                            run.Settings.AddSeries(resultTable);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    store.Close();
                    runList.SetSource(list);
                }
            }
        }

        public class ChartSettings
        {
            internal IData Run { get; set; }
            
            public List<Enabled<IDataViewModel>> Series { get; set; } = new List<Enabled<IDataViewModel>>();

            [Display("X-Axis")]
            [AvailableValues(nameof(AvailableResultColumns))]
            public string xAxis { get; set; }
            
            [Display("Y-Axis")]
            [AvailableValues(nameof(AvailableResultColumns))]
            public string yAxis { get; set; }
            
            [Browsable(false)]
            public List<string> AvailableResultColumns
            {
                get
                {
                    return Series.Where(s => s.IsEnabled).Select(s => s.Value).SelectMany(r => (r.Data as IResultTable)?.Columns).Select(c => c.Name).Distinct().ToList();
                }
            }
            
            public List<Plot> PlotCharts(string symbol = "*")
            {
                var list = new List<Plot>();
                
                foreach (var serie in Series.Where(s => s.IsEnabled))
                {
                    var resultTable = serie.Value.Data as IResultTable;

                    var xaxis = resultTable.Columns.FirstOrDefault(c => c.Name == xAxis);
                    var yaxis = resultTable.Columns.FirstOrDefault(c => c.Name == yAxis);
                    
                    if (xaxis == null || yaxis == null)
                        continue;
                    
                    var plot = new Plot();
                    for (int i = 0; i < xaxis.Data.Length; i++)
                    {
                        var x = Convert.ToDouble(xaxis.Data.GetValue(i));
                        var y = Convert.ToDouble(yaxis.Data.GetValue(i));
                        plot.Points[x] = y;
                    }
                    list.Add(plot);
                }
                return list;
            }
            
            public void AddSeries(IResultTable result)
            {
                Series.Add(new Enabled<IDataViewModel>(){ Value = new IDataViewModel(result, null), IsEnabled = true });
            }
        }
        
        [Display("Series")]
        public class IDataViewModel
        {
            public ChartSettings Settings { get; set; }
            private View Owner { get; set; }
            internal IData Data { get; set; }
            
            public IDataViewModel(IData run, View owner)
            {
                Data = run;
                Settings = new ChartSettings();
                Settings.Run = run;
                Owner = owner;
            }
            
            public override string ToString()
            {
                if (Data is IResultTable)
                    return Data.Parent.Name;
                
                var spacing = (Owner.Bounds.Width) / (Headers.Length);
                
                var id = Data.GetID().ToString();
                var name = Data.Name;
                var verdict = Data.Parameters.FirstOrDefault(p => p.Name == "Verdict")?.Value.ToString();
                var tags = Data.Parameters.FirstOrDefault(p => p.Name == "Tags")?.Value.ToString();

                var row = new StringBuilder();
                row.Append(id);
                row.Append(new string(' ', spacing - id.Length - 4));
                row.Append(name);
                row.Append(new string(' ', spacing - name.Length));
                row.Append(verdict);
                row.Append(new string(' ', spacing - verdict?.Length ?? 0));
                row.Append(tags);
                row.Append(new string(' ', spacing - tags?.Length ?? 0));
                
                return row.ToString();
            }
        }
    }
}
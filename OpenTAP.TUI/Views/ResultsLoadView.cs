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
    public class RunExplorerView : View
    {
        public static string[] Headers =  { "Run ID", "Name", "Verdict", "Tags" };
        
        private SelectorView runList;
        public Action<ListViewItemEventArgs> SelectedItemChanged;
        public Action<ListViewItemEventArgs> ItemMarkedChanged;
        public List<IDataViewModel> GetMarkedItems()
        {
            return runList.MarkedItems().Select(i => i as IDataViewModel).ToList();
        }

        public RunExplorerView()
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
            
            [Browsable(false)]
            public List<IDataViewModel> AvailableSeries { get; set; } = new List<IDataViewModel>();
            
            [AvailableValues(nameof(AvailableSeries))]
            public List<IDataViewModel> Series { get; set; } = new List<IDataViewModel>();
            
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
                    return Series.SelectMany(r => (r.Data as IResultTable)?.Columns).Select(c => c.Name).Distinct().ToList();
                }
            }
            
            public List<Plot> PlotCharts()
            {
                var list = new List<Plot>();
                
                foreach (var serie in Series)
                {
                    var resultTable = serie.Data as IResultTable;
                    if (resultTable == null)
                        continue;

                    var xaxis = resultTable.Columns.FirstOrDefault(c => c.Name == xAxis);
                    var yaxis = resultTable.Columns.FirstOrDefault(c => c.Name == yAxis);
                    
                    if (xaxis == null || yaxis == null)
                        continue;
                    
                    var plot = new Plot();
                    plot.Name = resultTable.Parent?.Name ?? resultTable.Name;
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
                var vm = new IDataViewModel(result, null);
                AvailableSeries.Add(vm);
                Series.Add(vm);

                if (xAxis == null)
                    xAxis = result.Columns.FirstOrDefault()?.Name;
                if (yAxis == null)
                    yAxis = result.Columns.FirstOrDefault(c => c.Name != xAxis)?.Name;
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
                
                var id = Data.Parameters.FirstOrDefault(p => p.Name == "Run ID")?.Value.ToString() ?? Data.GetID().ToString();
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
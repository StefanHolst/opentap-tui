using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;using OpenTap;
using OpenTap.Metrics;
using OpenTap.Tui.Views;
using Terminal.Gui;
using Terminal.Gui.Graphs;

namespace OpenTap.Tui.Windows
{
    public class MetricsWindow : BaseWindow, IMetricListener
    {
        private TreeView<MetricInfo> metricsTreeView;
        private TableView metricHistoryView;
        private GraphView metricGraphView;
        private ScatterSeries series;
        private PathAnnotation annotation;
    
        private MetricInfo selectedMetric = null;
        private Dictionary<MetricInfo, MetricViewModel> subscribedMetrics = new Dictionary<MetricInfo, MetricViewModel>();
        private List<MenuItem> helperActions;
        private HelperButtons helperButtons;

        private List<string> getGroups(MetricInfo metric)
        {
            return [metric.GroupName];
        }

        private string getTitle(MetricInfo metric)
        {
            return $"{(subscribedMetrics.ContainsKey(metric) ? "[x]" : "[ ]")} {metric.Name} ({metric.Type} - {metric.Kind})";
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Cancel))
            {
                var handled = base.ProcessKey(keyEvent);
                if (handled) return true;
                Application.RequestStop();
                return true;
            }
            
            return base.ProcessKey(keyEvent);
        }

        public MetricsWindow() : base("Metrics")
        {
            Modal = true;

            metricsTreeView = new TreeView<MetricInfo>(getTitle, getGroups);
            metricsTreeView.SelectedItemChanged += SelectedItemChanged;
            
            // Add metrics list
            var metricsFrameView = new FrameView("Sources")
            {
                Width = Dim.Percent(30),
                Height = Dim.Fill(1)
            };
            metricsFrameView.Add(metricsTreeView);
            Add(metricsFrameView);
            
            // Add metrics history
            metricHistoryView = new TableView();
            metricHistoryView.Style.AlwaysShowHeaders = true;
            // metricHistoryView.Style.ExpandLastRow = true;
            var metricsHistoryFrame = new FrameView("History")
            {
                X = Pos.Right(metricsFrameView),
                Width = Dim.Fill(),
                Height = Dim.Percent(50)
            };
            metricsHistoryFrame.Add(metricHistoryView);
            Add(metricsHistoryFrame);
            
            // Add metric graph
            metricGraphView = new GraphView();
            var metricsGraphFrame = new FrameView("Graph")
            {
                X = Pos.Right(metricsFrameView),
                Y = Pos.Bottom(metricsHistoryFrame),
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            metricsGraphFrame.Add(metricGraphView);
            Add(metricsGraphFrame);
            
            // Add series
            series = new ScatterSeries();
            metricGraphView.Series.Add(series);
            annotation = new PathAnnotation();
            metricGraphView.Annotations.Add(annotation);
            
            // Add helper buttons
            helperButtons = new HelperButtons()
            {
                Y = Pos.Bottom(metricsFrameView)
            };
            Add(helperButtons);
            helperActions = new List<MenuItem>();
            var subscribeAction = new MenuItem("Subscribe", "", () =>
            {
                if (selectedMetric == null)
                    return;
                
                if (subscribedMetrics.ContainsKey(selectedMetric))
                    subscribedMetrics.Remove(selectedMetric);
                else
                    subscribedMetrics[selectedMetric] = new MetricViewModel();

                MetricManager.Subscribe(this, subscribedMetrics.Keys.ToArray());
                metricsTreeView.RenderTreeView(true);
                
            }, shortcut: Key.F1);
            helperActions.Add(subscribeAction);
            var zoomAction = new MenuItem("Zoom In", "", () => Zoom((float)0.8), shortcut: Key.F3);
            helperActions.Add(zoomAction);
            var zoomOutAction = new MenuItem("Zoom Out", "", () => Zoom((float)1.25), shortcut: Key.F4);
            helperActions.Add(zoomOutAction);
            
            helperButtons.SetActions(helperActions, this);
            helperButtons.CanFocus = false;
            
            // load available metrics
            var metrics = MetricManager.GetMetricInfos().ToList();
            metricsTreeView.SetTreeViewSource(metrics);
            
            // If there is only a small set of metrics, subscribe to all of them
            if (metrics.Count < 10)
            {
                foreach (var metric in metrics)
                    subscribedMetrics[metric] = new MetricViewModel();

                MetricManager.Subscribe(this, subscribedMetrics.Keys.ToArray());
                metricsTreeView.RenderTreeView(true);
            }

            // Start a thread to update the history
            var updateThread = TapThread.Start(() =>
            {
                while (TapThread.Current.AbortToken.IsCancellationRequested == false)
                {
                    updateHistory();
                    TapThread.Sleep(100);
                }
            });
            Closing += _ =>
            {
                updateThread.Abort();
            };
        }

        void Zoom(float factor)
        {
            metricGraphView.CellSize = new PointF(
                metricGraphView.CellSize.X * factor,
                metricGraphView.CellSize.Y * factor
            );

            metricGraphView.AxisX.Increment *= factor;
            metricGraphView.AxisY.Increment *= factor;

            metricGraphView.SetNeedsDisplay();
        }

        private void SelectedItemChanged(ListViewItemEventArgs obj)
        {
            if (obj.Value is MetricInfo metric)
            {
                selectedMetric = metric;
                if (selectedMetric != null && helperActions.Count > 0 && helperActions[0] != null)
                    helperActions[0].Title = subscribedMetrics.ContainsKey(selectedMetric) ? "Unsubscribe" : "Subscribe";
            }
            else
                selectedMetric = null;
            
            // If no metric is selected, remove the subscribe button
            helperButtons.SetActions(selectedMetric == null ? helperActions.Skip(1).ToList() : helperActions, this);
            helperButtons.CanFocus = false;

            updateHistory();
        }

        private void updateHistory()
        {
            // Poll the metrics
            var metrics = MetricManager.PollMetrics(subscribedMetrics.Keys);
            foreach (var metric in metrics)
            {
                if (metric.Info != null && subscribedMetrics.TryGetValue(metric.Info, out var map))
                    map.AddMetric(metric);
            }
            
            // Update the UI
            Application.MainLoop.Invoke(() =>
            {
                if (selectedMetric == null || subscribedMetrics.ContainsKey(selectedMetric) == false)
                {
                    metricHistoryView.Table = new DataTable();
                    series.Points = new List<PointF>();
                    annotation.Points = new List<PointF>();
                    return;
                }
                
                if (subscribedMetrics.TryGetValue(selectedMetric, out var history) == false)
                    return;

                // Update history
                metricHistoryView.Table = history.MetricsTable;

                // Update graph
                series.Points = history.GraphMetrics;
                annotation.Points = history.GraphMetrics;
            });
        }
        
        public void OnPushMetric(IMetric table)
        {
            if (table.Info != null && subscribedMetrics.TryGetValue(table.Info, out var map))
                map.AddMetric(table);
        }
    }
}

public class MetricViewModel
{
    public List<PointF> GraphMetrics { get; } = new List<PointF>();
    public List<IMetric> Metrics { get; set; } = new List<IMetric>();
    public DataTable MetricsTable { get; set; } = new DataTable();

    public MetricViewModel()
    {
        MetricsTable.Columns.Add("Time");
        MetricsTable.Columns.Add("Value");
    }
    
    public void AddMetric(IMetric metric)
    {
        Metrics.Insert(0, metric);

        lock (MetricsTable)
        {
            var row = MetricsTable.NewRow();
            row[0] = metric.Time;
            row[1] = metric.Value;
            MetricsTable.Rows.InsertAt(row, 0);
        }
        
        if (metric.Info.Type == MetricType.Double)
            GraphMetrics.Add(new PointF(Metrics.Count, (float)(double)metric.Value));
    }
}

public class MetricsTestInstrument : Instrument, IMetricSource, IOnPollMetricsCallback
{
    [Metric]
    [Unit("I")] 
    public double X { get; private set; }

    [Metric]
    [Unit("V")]
    public double Y { get; private set; }

    [Metric]
    public string Z { get; private set; }

    [Metric]
    public bool OnOff { get; private set; }

    private int _offset = 0;
    public MetricsTestInstrument()
    {
        TapThread.Start(() =>
        {
            var xMetric = MetricManager.GetMetricInfo(this, nameof(X));
            var yMetric = MetricManager.GetMetricInfo(this, nameof(Y));
            var zMetric = MetricManager.GetMetricInfo(this, nameof(Z));
            var onOffMetric = MetricManager.GetMetricInfo(this, nameof(OnOff));
            for (int i = 0; i < 100; i++)
            {
                TapThread.Sleep(1000);
                _offset += 1;
                X = _offset;
                MetricManager.PushMetric(xMetric, X);
                MetricManager.PushMetric(yMetric, Math.Sin(_offset * 0.1));
                MetricManager.PushMetric(zMetric, $"Value: {_offset}");
                MetricManager.PushMetric(onOffMetric, _offset % 2 == 0);
            }
        });
    }

    public void OnPollMetrics(IEnumerable<MetricInfo> metrics)
    {
        _offset += 1;
        X = _offset;
        Y = Math.Sin(_offset * 0.1);
        Z = $"Value: {_offset}";
        OnOff = _offset % 2 == 0;
    }
}
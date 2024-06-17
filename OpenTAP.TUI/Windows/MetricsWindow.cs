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
        private TreeView<(MetricInfo, object)> metricsTreeView;
        private TableView metricHistoryView;
        private GraphView metricGraphView;
        private ScatterSeries series;
        private PathAnnotation annotation;
    
        private MetricInfo selectedMetric = null;
        private Dictionary<string, MetricViewModel> subscribedMetrics = new Dictionary<string, MetricViewModel>();
        private List<MenuItem> helperActions;
        private HelperButtons helperButtons;

        private List<string> getGroups((MetricInfo metric, object source) arg)
        {
            return [arg.metric.GroupName];
        }

        private string getTitle((MetricInfo metric, object source) metricInfo)
        {
            return $"{(subscribedMetrics.ContainsKey(metricInfo.metric.MetricFullName) ? "[x]" : "[ ]")} {metricInfo.metric.Name} ({metricInfo.metric.Kind} - {(metricInfo.metric.Ephemeral ? "Ephemeral" : "Persistent")})";
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

            metricsTreeView = new TreeView<(MetricInfo, object)>(getTitle, getGroups);
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
                
                if (subscribedMetrics.ContainsKey(selectedMetric.MetricFullName))
                    subscribedMetrics.Remove(selectedMetric.MetricFullName);
                else
                    subscribedMetrics[selectedMetric.MetricFullName] = new MetricViewModel();
                
                metricsTreeView.RenderTreeView(true);
                
            }, shortcut: Key.F1);
            helperActions.Add(subscribeAction);
            var zoomAction = new MenuItem("Zoom In", "", () => Zoom((float)0.8), shortcut: Key.F3);
            helperActions.Add(zoomAction);
            var zoomOutAction = new MenuItem("Zoom Out", "", () => Zoom((float)1.25), shortcut: Key.F4);
            helperActions.Add(zoomOutAction);
            
            helperButtons.SetActions(helperActions, this);
            helperButtons.CanFocus = false;

            // Register as consumer
            MetricManager.RegisterListener(this);
            
            // load available metrics
            var metrics = MetricManager.GetMetricInfos().ToList();
            metricsTreeView.SetTreeViewSource(metrics);
            
            // If there is only a small set of metrics, subscribe to all of them
            if (metrics.Count < 10)
            {
                foreach (var metric in metrics)
                    subscribedMetrics[metric.metric.MetricFullName] = new MetricViewModel();
                
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
            if (obj.Value is ValueTuple<MetricInfo, object> metric)
            {
                selectedMetric = metric.Item1;
                if (selectedMetric != null)
                    helperActions[0].Title = subscribedMetrics.ContainsKey(selectedMetric.MetricFullName) ? "Unsubscribe" : "Subscribe";
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
            Application.MainLoop.Invoke(() =>
            {
                if (selectedMetric == null || subscribedMetrics.ContainsKey(selectedMetric.MetricFullName) == false)
                {
                    metricHistoryView.Table = new DataTable();
                    series.Points = new List<PointF>();
                    annotation.Points = new List<PointF>();
                    return;
                }
                
                if (subscribedMetrics.TryGetValue(selectedMetric.MetricFullName, out var history) == false)
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
            if (table.Info != null && subscribedMetrics.ContainsKey(table.Info.MetricFullName))
                subscribedMetrics[table.Info.MetricFullName].AddMetric(table);
        }

        public IEnumerable<MetricInfo> GetInterest(IEnumerable<MetricInfo> allMetrics)
        {
            return allMetrics.Where(m => subscribedMetrics.ContainsKey(m.MetricFullName));
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

        var row = MetricsTable.NewRow();
        row[0] = metric.Time;
        row[1] = metric.Value;
        MetricsTable.Rows.InsertAt(row, 0);
        
        if (metric.Info.Kind == MetricKind.Double)
            GraphMetrics.Add(new PointF(Metrics.Count, (float)(double)metric.Value));
    }
}

public class MetricsTestInstrument : Instrument, IMetricSource
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
}
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class ResultsViewerWindow : Window
    {
        public static string[] Headers =  { "Run ID", "Name", "Verdict", "Source" };
        
        private SelectorView runList;
        private HelperButtons helperButtons;

        public ResultsViewerWindow() : base("Results Viewer")
        {
            Modal = true;

            // Add headers
            var frames = new List<FrameView>();
            for (int i = 0; i < Headers.Length; i++)
            {
                var header = Headers[i];
                var frame = new FrameView(header)
                {
                    Height = 2,
                    Width = Dim.Percent(100 / Headers.Length)
                };
                if (i > 0)
                    frame.X = Pos.Right(frames.LastOrDefault());

                if (i == Headers.Length - 1)
                    frame.Width = Dim.Fill();
                
                frames.Add(frame);
                Add(frame);
            }
            
            // Add runs
            runList = new SelectorView
            {
                Height = Dim.Fill(),
                Width = Dim.Fill()
            };
            var runFrame = new FrameView()
            {
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };
            runFrame.Add(runList);
            Add(runFrame);
            
            // Add helper buttons
            helperButtons = new HelperButtons
            {
                Width = Dim.Fill(),
                Height = 1,
                Y = Pos.AnchorEnd()-1
            };
            Add(helperButtons);
            
            // Add actions
            var actions = new List<MenuItem>();
            var runAction = new MenuItem("Plot Results", "", PlotResults, shortcut: Key.F5);
            actions.Add(runAction);
            
            runList.ItemMarkedChanged += (args =>
            {
                if (GetMarkedItems().Any())
                    helperButtons.SetActions(actions, this);
                else
                    helperButtons.SetActions(new List<MenuItem>(), this);
                
                Application.Refresh();
            });
            
            
            var runs = LoadRuns();
            runList.SetSource(runs);
        }

        private List<IRunViewModel> LoadRuns()
        {
            var runs = new Dictionary<long, IRunViewModel>();
            var stores = ResultSettings.Current.OfType<IResultStore>().ToList();

            foreach (var store in stores)
            {
                try
                {
                    store.Open();
                    var allEntries = store.GetEntries(SearchCondition.All(), new List<string>(), true);

                    foreach (var entry in allEntries)
                    {
                        if (entry.ObjectType == "Plan Run")
                        {
                            runs[entry.GetID()] = new IRunViewModel(entry, this, store);
                        }
                        else if ( entry is IResultTable resultTable)
                        {
                            // Find plan
                            // Find top most step run
                            var planRun = entry;
                            while (planRun.Parent != null)
                            {
                                planRun = planRun.Parent;
                            }

                            // Get plan vm
                            if (runs.ContainsKey(planRun.GetID()) == false)
                                runs[planRun.GetID()] = new IRunViewModel(planRun, this, store);
                            var run = runs[planRun.GetID()];

                            // If type is a result add it to the list
                            if (run.Series.ContainsKey(resultTable.Name) == false)
                                run.Series[resultTable.Name] = new List<IResultTable>();
                            run.Series[resultTable.Name].Add(resultTable);
                        }
                    }
                }
                finally
                {
                    store.Close();
                }
            }

            return runs.Select(r => r.Value).ToList();
        }
        
        List<IRunViewModel> GetMarkedItems()
        {
            return runList.MarkedItems().Select(i => i as IRunViewModel).ToList();
        }
        
        void PlotResults()
        {
            if (TuiAction.CurrentAction is TUI && IsCurrentTop == false || TuiAction.CurrentAction is TuiResults && Application.Current is EditWindow)
                return;
            
            var resultWindow = new ResultsWindow(GetMarkedItems());
            Application.Run(resultWindow);
            Remove(helperButtons);
            Add(helperButtons);
            runList.ItemMarkedChanged.Invoke(null);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (TuiAction.CurrentAction is TuiResults && KeyMapHelper.IsKey(keyEvent, KeyTypes.Close))
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                {
                    Application.RequestStop();
                }

                return true;
            }
            else if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Cancel))
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

    public class IRunViewModel
    {
        public Dictionary<string, List<IResultTable>> Series { get; set; } = new Dictionary<string, List<IResultTable>>();
        
        private View Owner { get; set; }
        private IData Run { get; set; }
        private IResultStore Store { get; set; }

        public IRunViewModel(IData run, View owner, IResultStore store)
        {
            Run = run;
            Owner = owner;
            Store = store;
        }
        
        public override string ToString()
        {
            if (Run is IResultTable)
                return Run.Parent.Name;
            
            var spacing = (Owner.Bounds.Width - 2) / ResultsViewerWindow.Headers.Length;
            if (spacing == 0)
                return "";
            
            var id = Run.Parameters.FirstOrDefault(p => p.Name == "Run ID")?.Value.ToString() ?? Run.GetID().ToString();
            var name = Run.Name;
            var verdict = Run.Parameters.FirstOrDefault(p => p.Name == "Verdict")?.Value.ToString();
            var source = Store.Name;

            var row = new StringBuilder();
            row.Append(id);
            row.Append(new string(' ', spacing - id.Length - 2));
            row.Append(name);
            row.Append(new string(' ', spacing - name.Length));
            row.Append(verdict);
            row.Append(new string(' ', spacing - (verdict?.Length ?? 0)));
            row.Append(source);
            row.Append(new string(' ', spacing - (source?.Length ?? 0)));
            
            return row.ToString();
        }
    }
}
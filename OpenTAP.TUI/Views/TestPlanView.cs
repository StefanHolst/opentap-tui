using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenTap;
using OpenTap.Plugins;
using OpenTap.Plugins.BasicSteps;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class TestPlanView : FrameView, IDisposable
    {
        private HashSet<ITestStep> moveSteps = new HashSet<ITestStep>();
        private bool injectStep = false;
        private MenuItem insertAction;
        private MenuItem runAction;
        private TreeView<ITestStep> treeView;
        private TestPlanRun testPlanRun;
        private bool PlanIsRunning = false;
        private readonly Recovery recoveryFile;
        
        ///<summary> Keeps track of the most recently focused step - even when the test plan is selected. </summary>
        ITestStep focusedStep;

        public TestPlan Plan => recoveryFile.Plan;

        public Action<object> SelectionChanged;

        public TestPlanView()
        {
            CanFocus = true;
            recoveryFile = new Recovery();
            recoveryFile.TestPlanChanged += LoadTestPlan;
            
            treeView = new TreeView<ITestStep>(getTitle, getChildren, getParent, createNode)
            {
                Height = Dim.Fill(),
                Width = Dim.Fill()
            };
            treeView.SetTreeViewSource(Plan.Steps);
            treeView.SelectedItemChanged += args =>
            {
                if (moveSteps.Any())
                    return;
                UpdateHelperButtons();
                focusedStep = args.Value as ITestStep;
                SelectionChanged?.Invoke(args.Value as ITestStepParent);
            };
            treeView.EnableFilter = true;
            treeView.FilterChanged += (filter) => 
            {
                UpdateTitle();
                if (filter == "solution")
                {
                    FocusMode.StartFocusMode(FocusModeUnlocks.Search, true);
                }
            };
            treeView.NodeVisibilityChanged += (node, expanded) => ChildItemVisibility.SetVisibility(node.Item, expanded ? ChildItemVisibility.Visibility.Visible : ChildItemVisibility.Visibility.Collapsed);
            treeView.KeyPress += TreeviewKeyPress;

            Add(treeView);
            
            MainWindow.UnsavedChangesCreated += UpdateTitle;
            UpdateTitle();
        }

        public void UpdateHelperButtons()
        {
            List<MenuItem> actions = new List<MenuItem>();
            actions.Add(new MenuItem("Test Plan Settings", "", () =>
            {
                SelectionChanged.Invoke(Plan);
            }, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.TestPlanSettings)));
            
            if (moveSteps.Any())
            {
                actions.Add(new MenuItem("Move Selection", "", () => MoveSelection(false), moveSteps.Any, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.InsertSelectedSteps)));
                actions.Add(new MenuItem("Move Selection As Children", "", () => MoveSelection(true), moveSteps.Any, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.InsertSelectedStepsAsChildren)));
            }
            else 
            {
                actions.Add(new MenuItem("Insert New Step", "", showAddStep, () => !moveSteps.Any(), shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.AddNewStep)));
                actions.Add(new MenuItem("Insert New Step Child", "", showInsertStep,
                    () => (treeView.SelectedObject?.GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null ||
                    treeView.SelectedObject?.GetType().GetCustomAttribute<AllowChildrenOfTypeAttribute>() != null) &&
                    moveSteps.Any() == false, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.InsertNewStep)));
            }
            
            actions.Add(new MenuItem(PlanIsRunning ? "Abort Test Plan" : "Run Test Plan", "", () =>
            {
                if (PlanIsRunning)
                {
                    if (MessageBox.Query(50, 7, "Abort Test Plan", "Are you sure you want to abort the test plan?", "Yes", "No") == 0)
                        AbortTestPlan();
                }
                else if (moveSteps.Any())
                {
                    switch (MessageBox.Query(50, 7, "Run selection", "Do you want to run only the selected test steps.", "Run selection", "Run entire plan", "Cancel"))
                    {
                        case 0:
                            RunTestPlan(true);
                            break;
                        case 1:
                            RunTestPlan(false);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    RunTestPlan(false);
                }
            }, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.RunTestPlan)));
            MainWindow.helperButtons.SetActions(actions, this);
        }

        private void UpdateTitle()
        {
            Title = KeyMapHelper.GetKeyName(KeyTypes.FocusTestPlan, Plan.Name) +
                (MainWindow.ContainsUnsavedChanges ? "*" : string.Empty) +
                (string.IsNullOrEmpty(treeView.Filter) ? string.Empty : $" - {treeView.Filter}") + 
                (PlanIsRunning ? " - Running " : string.Empty);
            for (int i = 0; PlanIsRunning && i < DateTime.Now.Second % 4; i++)
            {
                Title += ">";
            }
        }

        private void MoveSelection(bool inject) 
        {
            ITestStep selectedObject = treeView.SelectedObject;
            ITestStepParent insertParent = inject ? selectedObject : selectedObject.Parent;

            bool anyImmoveableSteps = false;
            foreach (var immoveableStep in moveSteps.Where(s => !TestStepList.AllowChild(TypeData.GetTypeData(insertParent), TypeData.GetTypeData(s))))
            {
                anyImmoveableSteps = true;
                TUI.Log.Warning($"{((ITestStep)insertParent).Name} cannot have children of type: {immoveableStep.TypeName}. De-select {immoveableStep.Name} to move steps.");
            }
            if (anyImmoveableSteps)
                return;

            Dictionary<ITestStep, int> stepIndices = TestStepOrder();
            foreach (var step in moveSteps)
            {
                step.Parent.ChildTestSteps.Remove(step);
            }
            
            int insertIndex = inject ? 0 : insertParent.ChildTestSteps.IndexOf(selectedObject) + 1;

            foreach (var step in moveSteps.OrderBy((s) => stepIndices[s]).Reverse())
            {
                insertParent.ChildTestSteps.Insert(insertIndex, step);
            }

            MainWindow.ContainsUnsavedChanges = true;
            moveSteps.Clear();
            ChildItemVisibility.SetVisibility(insertParent, ChildItemVisibility.Visibility.Visible);
            Update(true);
            treeView.SelectedObject = injectStep ? (ITestStep)insertParent : selectedObject;
            Update(true);
        }

        private Dictionary<ITestStep, int> TestStepOrder()
        {
            Dictionary<ITestStep, int> stepOrder = new Dictionary<ITestStep, int>();
            int stepCount = 0;
            TestStepOrderRec(Plan.ChildTestSteps);
            return stepOrder;

            void TestStepOrderRec(TestStepList steps){
                foreach (var step in steps)
                {
                    stepOrder.Add(step, ++stepCount);
                    TestStepOrderRec(step.ChildTestSteps);
                }
            }
        }

        private void TreeviewKeyPress(KeyEventEventArgs kbEvent)
        {
            var kb = kbEvent.KeyEvent;
            if (Plan.IsRunning)
                return;

            if ((kb.Key == Key.CursorUp || kb.Key == Key.CursorDown) && injectStep)
            {
                injectStep = false;
                Update(true);
                kbEvent.Handled = true;
            }
            if (KeyMapHelper.IsKey(kb, KeyTypes.SelectStep) && treeView.SelectedObject != null)
            {
                if (moveSteps.Contains(treeView.SelectedObject))
                {
                    moveSteps.Remove(treeView.SelectedObject);
                }
                else
                {
                    moveSteps.Add(treeView.SelectedObject);
                }

                if (moveSteps.Any())
                    SelectionChanged?.Invoke(moveSteps.ToArray());
                else
                    SelectionChanged?.Invoke(treeView.SelectedObject);
                Update(true);
                kbEvent.Handled = true;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.DeleteStep))
            {
                if (treeView.SelectedObject != null)
                {
                    var itemToRemove = treeView.SelectedObject;
                    itemToRemove.Parent.ChildTestSteps.Remove(itemToRemove);
                    Update(true);
                }
                kbEvent.Handled = true;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.Save))
            {
                SaveTestPlan(Plan.Path);
                kbEvent.Handled = true;
            }
            if (KeyMapHelper.IsKey(kb, KeyTypes.SaveAs))
            {
                SaveTestPlan(null);
                kbEvent.Handled = true;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.Open))
            {
                LoadTestPlan();
                kbEvent.Handled = true;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.Copy))
            {
                // Copy
                var copyStep = treeView.SelectedObject;
                var serializer = new TapSerializer();
                var xml = serializer.SerializeToString(copyStep);

                Clipboard.Contents = xml;

                kbEvent.Handled = true;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.Paste) && Clipboard.Contents != null && treeView.SelectedObject != null) // 86 = V
            {
                // Paste
                var toItem = treeView.SelectedObject;
                var toIndex = toItem.Parent.ChildTestSteps.IndexOf(toItem) + 1;

                // Serialize Deserialize step to get a new instance
                var serializer = new TapSerializer();
                serializer.GetSerializer<TestStepSerializer>().AddKnownStepHeirarchy(Plan);
                var newStep = serializer.DeserializeFromString(Clipboard.Contents.ToString(), TypeData.FromType(typeof(TestPlan)), path: Plan.Path) as ITestStep;

                if (newStep != null)
                {
                    toItem.Parent.ChildTestSteps.Insert(toIndex, newStep);
                    Update(true);
                    treeView.SelectedObject = newStep;
                    Update();
                }
                MainWindow.ContainsUnsavedChanges = true;

                kbEvent.Handled = true;
            }
        }

        /// <summary>
        /// Overrides SetFocus when called directly.
        /// </summary>
        public new void SetFocus() // new used as SetFocus is not virtual in gui.cs, but it works just as well.
        {
            base.SetFocus();
            if (moveSteps.Any())
                SelectionChanged?.Invoke(moveSteps.ToArray());
            else if(focusedStep != null)
                SelectionChanged.Invoke(focusedStep);
        }

        string getTitle(ITestStep step)
        {
            string title = step.GetFormattedName();
            if (moveSteps.Contains(step))
                title += " *";
            else if (injectStep && treeView.SelectedObject == step)
                title += " >";
            return title;
        }
        List<ITestStep> getChildren(ITestStep step)
        {
            return step.ChildTestSteps.ToList();
        }
        ITestStep getParent(ITestStep step)
        {
            return step.Parent as ITestStep;
        }
        TreeViewNode<ITestStep> createNode(ITestStep step)
        {
            return new TreeViewNode<ITestStep>(step, treeView)
            {
                IsExpanded = ChildItemVisibility.GetVisibility(step) == ChildItemVisibility.Visibility.Visible,
                AlwaysDisplayExpandState = step.GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null || step.GetType().GetCustomAttribute<AllowChildrenOfTypeAttribute>() != null,
            };
        }

        public override bool OnEnter(View view)
        {
            Update();
            return base.OnEnter(view);
        }

        public void Update(bool noCache = false)
        {
            TuiAction.AssertTuiThread();
            treeView.RenderTreeView(noCache);
            UpdateHelperButtons();
        }
        
        public void LoadTestPlan()
        {
            var dialog = new OpenDialog("Open a TestPlan", "Open");
            Application.Run(dialog);
            
            // If not path is selected
            if (dialog.DirectoryPath == null || dialog.FilePath == null)
                return;
            
            var path = Path.Combine(dialog.DirectoryPath.ToString(), dialog.FilePath.ToString());
            if (File.Exists(path) && dialog.Canceled == false)
            {
                try
                {
                    LoadTestPlan(path);
                }
                catch
                {
                    TUI.Log.Warning($"Could not load test plan '{path}'.");
                }
            }
        }

        public void LoadTestPlan(string path)
        {
            recoveryFile.Plan = TestPlan.Load(path);
        }

        private void LoadTestPlan(TestPlan plan)
        {
            treeView.SetTreeViewSource(Plan.Steps);
            UpdateTitle();
        }
        
        public void NewTestPlan()
        {
            recoveryFile.Plan = new TestPlan();
            treeView.SetTreeViewSource(Plan.Steps);
            MainWindow.ContainsUnsavedChanges = true;
        }
        public void SaveTestPlan(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                var dialog = new SaveDialog("Save TestPlan", "Where do you want to save the TestPlan?");
                Application.Run(dialog);
                if (dialog.FileName != null)
                    path = Path.Combine(dialog.DirectoryPath.ToString(), dialog.FilePath.ToString());
            }

            if (string.IsNullOrWhiteSpace(path) == false)
            {
                Plan.Save(path);
                MainWindow.ContainsUnsavedChanges = false;
                TUI.Log.Info($"Saved test plan to '{Plan.Path}'.");
            }
        }
        
        private void AddNewStep(ITypeData type)
        {
            try
            {
                if (Plan.Steps.Any() == false)
                {
                    var newStep = type.CreateInstance() as ITestStep;
                    Plan.ChildTestSteps.Add(newStep);
                    Update(true);
                }
                else if (treeView.SelectedObject != null)
                {
                    var newStep = type.CreateInstance() as ITestStep;
                    var index = treeView.SelectedObject.Parent.ChildTestSteps.IndexOf(treeView.SelectedObject);
                    treeView.SelectedObject.Parent?.ChildTestSteps.Insert(index, newStep);
                    Update(true);
                    treeView.SelectedObject = newStep;
                }
                MainWindow.ContainsUnsavedChanges = true;
            }
            catch(Exception ex)
            {
                TUI.Log.Debug(ex);
                TUI.Log.Error(ex.Message);
            }
        }
        private void InsertNewChildStep(ITypeData type)
        {
            if (Plan.Steps.Any() == false)
            {
                AddNewStep(type);
                return;
            }

            try
            {
                if (treeView.SelectedObject == null)
                    return;
                
                var newStep = type.CreateInstance() as ITestStep;
                treeView.SelectedObject.ChildTestSteps.Add(newStep);
                Update(true);
                treeView.ExpandObject(treeView.SelectedObject);
                treeView.SelectedObject = newStep;
                Update();
            }
            catch (Exception ex)
            {
                TUI.Log.Error(ex);
            }
        }
        private void showAddStep()
        {
            var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "New Step", TypeData.GetTypeData(treeView.SelectedObject?.Parent ?? Plan));
            Application.Run(newStep);
            if (newStep.PluginType != null)
                AddNewStep(newStep.PluginType);
        }

        private void showInsertStep()
        {
            var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "New Step Child", TypeData.GetTypeData(treeView.SelectedObject));
            Application.Run(newStep);
            if (newStep.PluginType != null)
                InsertNewChildStep(newStep.PluginType);
        }
        
        private TapThread testPlanThread;
        private void AbortTestPlan()
        {
            if (Plan.IsRunning)
            {
                testPlanThread.Abort();
                Update();
            }
        }
        private void RunTestPlan(bool runSelection)
        {
            PlanIsRunning = true;
            Update();
            this.Plan.PrintTestPlanRunSummary = true;
            testPlanThread = TapThread.Start(() =>
            {
                // Run testplan and show progress bar
                testPlanRun = Plan.Execute(ResultSettings.Current, stepsOverride: runSelection ? moveSteps : null);
                Application.MainLoop.Invoke(() =>
                    {
                        PlanIsRunning = false;
                        UpdateHelperButtons();
                        Update();
                    });
            });
            
            Task.Run(() =>
            {
                DateTime startTime = DateTime.Now;
                while (PlanIsRunning)
                {
                    Application.MainLoop.Invoke(UpdateTitle);
                    Thread.Sleep(1000);
                }
                DateTime dt = DateTime.Now;
                if (dt - startTime >= TimeSpan.FromSeconds(59) && dt - startTime <= TimeSpan.FromSeconds(61) && !Plan.EnabledSteps.Any(s => s is DelayStep))
                {
                    FocusMode.StartFocusMode(FocusModeUnlocks.Wait, true);
                }
                
                Application.MainLoop.Invoke(UpdateTitle);
            });
        }

        public void Dispose()
        {
            recoveryFile.Dispose();
        }
    }

    public class Recovery : IDisposable
    {
        public class RecFile {
            public string FilePath { get; set; }
            public string TestPlan { get; set; }
        }
        public RecFile file = new RecFile();
        public string FilePath { get => file.FilePath; set => file.FilePath = value; }
        public string TestPlan { get => file.TestPlan; set => file.TestPlan = value; }

        private TestPlan plan = new TestPlan();
        public TestPlan Plan
        {
            get
            {
                return plan;
            }
            set 
            {
                plan = value;
                FilePath = plan.Path;
                Save();
                TestPlanChanged?.Invoke(plan);
            }
        }

        private Stream recStream;

        public event Action<TestPlan> TestPlanChanged;

        public Recovery()
        {
            recStream = File.OpenWrite($".{Process.GetCurrentProcess().Id}.TuiRecovery");
            MainWindow.UnsavedChangesCreated += Save;
            Application.MainLoop.Invoke(() =>
            {
                if (string.IsNullOrEmpty(plan.Path))
                {
                    if (!Load())
                    {
                        Plan = new TestPlan();
                    }
                }
            });
        }

        private static TapSerializer TapSerializer = new TapSerializer();

        public void Save()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Plan.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(ms))
                {
                    TestPlan = sr.ReadToEnd();
                }
            }

            recStream.Seek(0, SeekOrigin.Begin);
            TapSerializer.Serialize(recStream, file);
        }

        public bool Load()
        {
            string[] files = Directory.GetFiles("./", ".*.TuiRecovery");
            if (files.Length == 0)
                return false;

            foreach (var file in files)
            {
                RecFile recfile = null;
                try
                {
                    recfile = TapSerializer.DeserializeFromFile(file, type: TypeData.FromType(typeof(Recovery))) as RecFile;
                    if (recfile == null)
                        continue;
                }
                catch
                {
                    continue;
                }
                TUI.Log.Debug("Recovery test plan detected." + file);
                File.Delete(file);
                MainWindow.ContainsUnsavedChanges = true;

                using (MemoryStream ms = new MemoryStream(recfile.TestPlan.Length * 2))
                {
                    StreamWriter sw = new StreamWriter(ms);
                    sw.Write(recfile.TestPlan);
                    sw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    Plan = OpenTap.TestPlan.Load(ms, recfile.FilePath);
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            recStream.Dispose();
            File.Delete($".{Process.GetCurrentProcess().Id}.TuiRecovery");
        }
    }
}

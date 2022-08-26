using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenTap.Plugins;
using OpenTap.Plugins.BasicSteps;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class TestPlanView : FrameView
    {
        private ITestStep moveStep = null;
        private bool injectStep = false;
        private List<MenuItem> actions;
        private MenuItem insertAction;
        private MenuItem runAction;
        private TreeView<ITestStep> treeView;
        private TestPlanRun testPlanRun;
        private bool PlanIsRunning = false;
        
        ///<summary> Keeps track of the most recently focused step - even when the test plan is selected. </summary>
        ITestStep focusedStep;
        
        public TestPlan Plan { get; set; } = new TestPlan();

        public Action<ITestStepParent> SelectionChanged;

        public TestPlanView()
        {
            CanFocus = true;
            
            treeView = new TreeView<ITestStep>(getTitle, getChildren, getParent, createNode)
            {
                Height = Dim.Fill(),
                Width = Dim.Fill()
            };
            treeView.SetTreeViewSource(Plan.Steps);
            treeView.SelectedItemChanged += args =>
            {
                focusedStep = args.Value as ITestStep;
                MainWindow.helperButtons.SetActions(actions, this);
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
            Add(treeView);
            
            actions = new List<MenuItem>();
            runAction = new MenuItem("Run Test Plan", "", () =>
            {
                if (PlanIsRunning)
                {
                    if (MessageBox.Query(50, 7, "Abort Test Plan", "Are you sure you want to abort the test plan?", "Yes", "No") == 0)
                        AbortTestPlan();
                }
                else
                    RunTestPlan();
            }, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.RunTestPlan));
            actions.Add(runAction);
            actions.Add(new MenuItem("Insert New Step", "", showAddStep, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.AddNewStep)));
            insertAction = new MenuItem("Insert New Step Child", "", showInsertStep, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.InsertNewStep));
            insertAction.CanExecute += () => treeView.SelectedObject?.GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null ||
                treeView.SelectedObject?.GetType().GetCustomAttribute<AllowChildrenOfTypeAttribute>() != null;
            actions.Add(insertAction);
            actions.Add(new MenuItem("Test Plan Settings", "", () =>
            {
                SelectionChanged.Invoke(Plan);
            }, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.TestPlanSettings)));
            MainWindow.UnsavedChangesCreated += UpdateTitle;
            UpdateTitle();
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

        /// <summary>
        /// Overrides SetFocus when called directly.
        /// </summary>
        public new void SetFocus() // new used as SetFocus is not virtual in gui.cs, but it works just as well.
        {
            base.SetFocus();
            if(focusedStep != null)
                SelectionChanged.Invoke(focusedStep);
        }

        string getTitle(ITestStep step)
        {
            string title = step.GetFormattedName();
            if (moveStep == step)
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
                IsExpanded = ChildItemVisibility.GetVisibility(step) == ChildItemVisibility.Visibility.Visible
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
            MainWindow.helperButtons.SetActions(actions, this);
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
            Plan = TestPlan.Load(path);
            treeView.SetTreeViewSource(Plan.Steps);
            UpdateTitle();
        }
        
        public void NewTestPlan()
        {
            Plan = new TestPlan();
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
            var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "New Step", null);
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
                runAction.Title = "Run Test Plan";
                Update();
            }
        }
        private void RunTestPlan()
        {
            PlanIsRunning = true;
            runAction.Title = "Abort Test Plan";
            Update();
            this.Plan.PrintTestPlanRunSummary = true;
            testPlanThread = TapThread.Start(() =>
            {
                // Run testplan and show progress bar
                testPlanRun = Plan.Execute();
                Application.MainLoop.Invoke(() =>
                    {
                        PlanIsRunning = false;
                        runAction.Title = "Run Test Plan";
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

        public override bool ProcessKey(KeyEvent kb)
        {
            if (Plan.IsRunning)
                return base.ProcessKey(kb);
            
            if ((kb.Key == Key.CursorUp || kb.Key == Key.CursorDown) && injectStep)
            {
                injectStep = false;
                base.ProcessKey(kb);
                Update(true);
                return true;
            }
            
            if (KeyMapHelper.IsKey(kb, KeyTypes.DeleteStep))
            {
                if (treeView.SelectedObject != null)
                {
                    var itemToRemove = treeView.SelectedObject;
                    itemToRemove.Parent.ChildTestSteps.Remove(itemToRemove);
                    Update(true);
                }
                return true;
            }
            
            if (kb.Key == Key.CursorRight && moveStep != null && (treeView.SelectedObject?.GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null || treeView.SelectedObject?.GetType().GetCustomAttribute<AllowChildrenOfTypeAttribute>() != null))
            {
                injectStep = true;
                Update(true);
                return true;
            }

            if (kb.Key == Key.Space)
            {
                if (Plan.ChildTestSteps.Count == 0 || treeView.SelectedObject == null)
                    return false;

                if (moveStep == null)
                {
                    moveStep = treeView.SelectedObject;
                    Update(true);
                }
                else if (moveStep == treeView.SelectedObject)
                {
                    moveStep = null;
                    injectStep = false;
                    Update(true);
                }
                else
                {
                    var currentIndex = treeView.SelectedObject.Parent.ChildTestSteps.IndexOf(treeView.SelectedObject);
                    
                    if (injectStep)
                    {
                        moveStep.Parent.ChildTestSteps.Remove(moveStep);
                        treeView.SelectedObject.ChildTestSteps.Add(moveStep);
                        treeView.ExpandObject(treeView.SelectedObject);
                    }
                    else
                    {
                        moveStep.Parent.ChildTestSteps.Remove(moveStep);
                        treeView.SelectedObject.Parent.ChildTestSteps.Insert(currentIndex, moveStep);
                    }
                    
                    Update(true);
                    treeView.SelectedObject = moveStep;
                    moveStep = null;
                    injectStep = false;
                    Update(true);
                    MainWindow.ContainsUnsavedChanges = true;
                }
                
                return true;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.Save))
            {
                SaveTestPlan(Plan.Path);
                return true;
            }
            if (KeyMapHelper.IsKey(kb, KeyTypes.SaveAs))
            {
                SaveTestPlan(null);
                return true;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.Open))
            {
                LoadTestPlan();
                return true;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.AddNewStep))
            {
                showAddStep();
                return true;
            }
            if (KeyMapHelper.IsKey(kb, KeyTypes.InsertNewStep) && (treeView.SelectedObject?.GetType().GetCustomAttribute<AllowChildrenOfTypeAttribute>() != null || treeView.SelectedObject?.GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null))
            {
                showInsertStep();
                return true;
            }

            if (KeyMapHelper.IsKey(kb, KeyTypes.Copy))
            {
                // Copy
                var copyStep = treeView.SelectedObject;
                var serializer = new TapSerializer();
                var xml = serializer.SerializeToString(copyStep);
            
                Clipboard.Contents = xml;
                
                return true;
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
                
                return true;
            }
            
            return base.ProcessKey(kb);
        }
    }
}

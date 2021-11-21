using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenTap.Plugins;
using OpenTap.Tui.Windows;
using Terminal.Gui;
using Terminal.Gui.Trees;

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
        public TestPlan Plan { get; set; } = new TestPlan();

        public Action<ITestStepParent> SelectionChanged;

        public TestPlanView()
        {
            CanFocus = true;
            Title = "Test Plan";
            
            treeView = new TreeView<ITestStep>()
            {
                Height = Dim.Fill(),
                Width = Dim.Fill()
            };
            treeView.AspectGetter = getTitle;
            treeView.TreeBuilder = new DelegateTreeBuilder<ITestStep> (getChildren, canExpand);
            treeView.SelectionChanged += (sender, args) =>
            {
                SelectionChanged?.Invoke(args.NewValue);
            };
            treeView.AddObjects(Plan.Steps);
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
            });
            actions.Add(runAction);
            actions.Add(new MenuItem("Insert New Step", "", showAddStep));
            insertAction = new MenuItem("Insert New Step Child", "", () =>
            {
                var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "New Step Child");
                Application.Run(newStep);
                if (newStep.PluginType != null)
                {
                    InsertNewChildStep(newStep.PluginType);
                }
            });
            insertAction.CanExecute += () => treeView.SelectedObject?.GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null;
            actions.Add(insertAction);
            actions.Add(new MenuItem("Test Plan Settings", "", () =>
            {
                SelectionChanged.Invoke(Plan);
            }));
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
        bool canExpand(ITestStep step)
        {
            return step.ChildTestSteps.Any();
        }

        public override bool OnEnter(View view)
        {
            Update();
            return base.OnEnter(view);
        }

        public void Update()
        {
            var selected = treeView.SelectedObject;
            treeView.ClearObjects();
            treeView.AddObjects(Plan.Steps);
            treeView.SelectedObject = selected ?? Plan.Steps.FirstOrDefault();
            
            treeView.RebuildTree();
            
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
                    Plan = TestPlan.Load(path);
                    Update();
                }
                catch
                {
                    TUI.Log.Warning($"Could not load test plan '{path}'.");
                }
            }
        }
        public void NewTestPlan()
        {
            Plan = new TestPlan();
            Update();
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
                TUI.Log.Info($"Saved test plan to '{Plan.Path}'.");
            }
        }
        
        private void AddNewStep(ITypeData type)
        {
            try
            {
                // TODO: add step at selected step
                
                if (Plan.Steps.Any() == false)
                    Plan.ChildTestSteps.Add(type.CreateInstance() as ITestStep);
                else
                    treeView.SelectedObject?.Parent?.ChildTestSteps.Add(type.CreateInstance() as ITestStep);
                
                Update();
            }
            catch(Exception ex)
            {
                TUI.Log.Error(ex);
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
                treeView.SelectedObject?.ChildTestSteps.Add(type.CreateInstance() as ITestStep);
                Update();
            }
            catch (Exception ex)
            {
                TUI.Log.Error(ex);
            }
        }
        private void showAddStep()
        {
            var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "New Step");
            Application.Run(newStep);
            if (newStep.PluginType != null)
            {
                AddNewStep(newStep.PluginType);
            }
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
                PlanIsRunning = false;
                runAction.Title = "Run Test Plan";
                Update();
            });
            
            Task.Run(() =>
            {
                while (PlanIsRunning)
                {
                    Application.MainLoop.Invoke(() => Title = $"Test Plan - Running ");
                    Thread.Sleep(1000);
                    
                    for (int i = 0; i < 3 && PlanIsRunning; i++)
                    {
                        Application.MainLoop.Invoke(() => Title += ">");
                        Thread.Sleep(1000);
                    }
                }
                
                Application.MainLoop.Invoke(() => Title = "Test Plan");
            });
        }

        public override bool ProcessKey(KeyEvent kb)
        {
            // if (Application.Current.MostFocused != this)
            //     return base.ProcessKey(kb);
            
            if (kb.Key == Key.CursorUp || kb.Key == Key.CursorDown)
            {
                injectStep = false;
                base.ProcessKey(kb);
                Update();
                return true;
            }
            
            if (Plan.IsRunning)
                return base.ProcessKey(kb);
            
            if (kb.Key == Key.DeleteChar)
            {
                if (treeView.SelectedObject != null)
                {
                    treeView.SelectedObject.Parent.ChildTestSteps.Remove(treeView.SelectedObject);
                    Update();
                }
                return true;
            }
            
            if (kb.Key == Key.CursorRight && moveStep != null && treeView.SelectedObject?.GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null)
            {
                injectStep = true;
                Update();
                return true;
            }

            if (kb.Key == Key.Space)
            {
                if (Plan.ChildTestSteps.Count == 0 || treeView.SelectedObject == null)
                    return false;

                if (moveStep == null)
                {
                    moveStep = treeView.SelectedObject;
                    Update();
                }
                else if (moveStep == treeView.SelectedObject)
                {
                    moveStep = null;
                    injectStep = false;
                }
                else
                {
                    moveStep.Parent.ChildTestSteps.Remove(moveStep);
                    var currentIndex = treeView.SelectedObject.Parent.ChildTestSteps.IndexOf(treeView.SelectedObject);

                    if (injectStep)
                        treeView.SelectedObject.ChildTestSteps.Add(moveStep);
                    else
                        treeView.SelectedObject.Parent.ChildTestSteps.Insert(currentIndex, moveStep);
                    
                    moveStep = null;
                    injectStep = false;
                    Update();
                }
                
                return true;
                
                
                // if (moveIndex == -1)
                // {
                //     moveIndex = SelectedItem;
                //     Update();
                //     return true;
                // }
                // else
                // {
                //     var flatPlan = FlattenPlan();
                //
                //     var fromItem = flatPlan[moveIndex];
                //     var toItem = flatPlan[SelectedItem];
                //
                //     var toIndex = toItem.Parent.ChildTestSteps.IndexOf(toItem);
                //     var flatIndex = flatPlan.IndexOf(toItem);
                //
                //     if (IsParent(toItem, fromItem) == false)
                //     {
                //         fromItem.Parent.ChildTestSteps.Remove(fromItem);
                //
                //         if (injectStep)
                //             toItem.ChildTestSteps.Add(fromItem);
                //         else
                //             toItem.Parent.ChildTestSteps.Insert(toIndex, fromItem);
                //     }
                //
                //     injectStep = false;
                //     moveIndex = -1;
                //     Update();
                //     SelectedItem = flatIndex;
                //     return true;
                // }
            }

            if (kb.Key == Key.CursorRight || kb.Key == Key.CursorLeft)
                return true;

            if (kb.Key == (Key.S | Key.CtrlMask))
            {
                SaveTestPlan(kb.IsShift ? null : Plan.Path);
                return true;
            }

            if (kb.Key == (Key.O | Key.CtrlMask))
            {
                LoadTestPlan();
                return true;
            }

            if (kb.Key == (Key.T | Key.CtrlMask))
            {
                showAddStep();
                return true;
            }

            // if (kb.IsShift && kb.Key == (Key.C|Key.CtrlMask) || kb.KeyValue == 67) // 67 = C
            // {
            //     // Copy
            //     var flatPlan = FlattenPlan();
            //     var copyStep = flatPlan[SelectedItem];
            //     var serializer = new TapSerializer();
            //     var xml = serializer.SerializeToString(copyStep);
            //
            //     Clipboard.Contents = xml;
            //     
            //     return true;
            // }

            // if ((kb.IsShift && kb.Key == (Key.V|Key.CtrlMask) || kb.KeyValue == 86) && Clipboard.Contents != null && SelectedItem > -1 ) // 86 = V
            // {
            //     // Paste
            //     var flatPlan = FlattenPlan();
            //     if (flatPlan.Count == 0)
            //         return true;
            //     
            //     var toItem = flatPlan[SelectedItem];
            //     var toIndex = toItem.Parent.ChildTestSteps.IndexOf(toItem) + 1;
            //     var flatIndex = flatPlan.IndexOf(toItem);
            //
            //     // Serialize Deserialize step to get a new instance
            //     var serializer = new TapSerializer();
            //     serializer.GetSerializer<TestStepSerializer>().AddKnownStepHeirarchy(Plan);
            //     var newStep = serializer.DeserializeFromString(Clipboard.Contents.ToString(), TypeData.FromType(typeof(TestPlan)), path: Plan.Path) as ITestStep;
            //     
            //     if (newStep != null)
            //     {
            //         var existingStep = toItem.Parent.ChildTestSteps.ElementAtOrDefault(toIndex-1);
            //         toItem.Parent.ChildTestSteps.Insert(toIndex, newStep);
            //         Update();
            //         var addedSteps = FlattenSteps(new[] {existingStep}).Count;
            //         SelectedItem = flatIndex + addedSteps;
            //     }
            //     
            //     return true;
            // }
            
            return base.ProcessKey(kb);
        }
    }
}

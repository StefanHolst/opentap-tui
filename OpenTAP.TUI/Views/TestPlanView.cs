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

namespace OpenTap.Tui.Views
{
    public class TestPlanView : ListView
    {
        private int moveIndex = -1;
        private bool injectStep = false;
        private List<MenuItem> actions;
        private MenuItem insertAction;
        private MenuItem runAction;
        public TestPlan Plan { get; set; } = new TestPlan();
        public FrameView TestPlanFrame { get; set; }

        private List<string> ExpandItems()
        {
            List<string> _ExpandItem(TestStepList steps, int level = 0)
            {
                List<string> list = new List<string>();
                foreach (var item in steps)
                {
                    list.Add($"{new String(' ', level * 2)}{item.GetFormattedName()}");
                    if (item.ChildTestSteps.Any())
                        list.AddRange(_ExpandItem(item.ChildTestSteps, level + 1));
                }
                return list;
            }

            var allsteps = _ExpandItem(Plan.ChildTestSteps);
            if (moveIndex > -1)
                allsteps[moveIndex] += " *";
            if (injectStep)
                allsteps[SelectedItem] += " >";

            return allsteps;
        }
        
        
        List<ITestStep> FlattenSteps(IEnumerable<ITestStep> steps)
        {
            var list = new List<ITestStep>();
            foreach (var item in steps)
            {
                list.Add(item);
                if (item.ChildTestSteps.Any())
                    list.AddRange(FlattenSteps(item.ChildTestSteps));
            }
            return list;
        }
        private List<ITestStep> FlattenPlan()
        {
            return FlattenSteps(Plan.ChildTestSteps);
        }

        public ITestStep SelectedStep
        {
            get
            {
                var plan = FlattenPlan();
                if (plan.Any() == false)
                    return null;

                return plan[SelectedItem];
            }
        }

        public TestPlanView()
        {
            CanFocus = true;
            
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
            actions.Add(new MenuItem("Insert New Step", "", () =>
            {
                var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "New Step");
                Application.Run(newStep);
                if (newStep.PluginType != null)
                {
                    AddNewStep(newStep.PluginType);
                }
            }));
            insertAction = new MenuItem("Insert New Step Child", "", () =>
            {
                var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "New Step Child");
                Application.Run(newStep);
                if (newStep.PluginType != null)
                {
                    InsertNewChildStep(newStep.PluginType);
                }
            });
            insertAction.CanExecute += () => SelectedStep?.GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null;
            actions.Add(insertAction);
            actions.Add(new MenuItem("Test Plan Settings", "", () =>
            {
                InvokeSelectedItemChanged(new ListViewItemEventArgs(0, Plan));
            }));
        }

        public override bool OnEnter(View view)
        {
            Update();
            return base.OnEnter(view);
        }

        public void Update()
        {
            var index = SelectedItem;
            var top = TopItem;
            SetSource(ExpandItems());
            if (top > 0 && top < Source.Count)
                TopItem = top;
            if (Source.Count > 0)
                SelectedItem = (index > Source.Count - 1 ? Source.Count - 1 : index);
            
            // Make sure to invoke event when the last item is deleted
            if (Source.Count == 0)
                OnSelectedChanged(); // TODO: Test
                // SelectedItemChanged.Invoke(null);

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
        public void AddNewStep(ITypeData type)
        {
            try
            {
                var flatplan = FlattenPlan();
                if (flatplan.Count == 0)
                {
                    Plan.ChildTestSteps.Add(type.CreateInstance() as ITestStep);
                    Update();
                    return;
                }
                
                var step = flatplan[SelectedItem];
                var index = step.Parent.ChildTestSteps.IndexOf(step);
                step.Parent.ChildTestSteps.Insert(index + 1, type.CreateInstance() as ITestStep);
                
                Update();
                SelectedItem = flatplan.IndexOf(step) + 1;
            }
            catch(Exception ex)
            {
                TUI.Log.Error(ex);
            }
        }
        public void InsertNewChildStep(ITypeData type)
        {
            var flatplan = FlattenPlan();
            if (flatplan.Count == 0)
            {
                AddNewStep(type);
                return;
            }

            try
            {
                var step = flatplan[SelectedItem];
                step.ChildTestSteps.Add(type.CreateInstance() as ITestStep);
                Update();
            }
            catch (Exception ex)
            {
                TUI.Log.Error(ex);
            }
        }

        private TestPlanRun testPlanRun;
        public bool PlanIsRunning = false;
        private TapThread testPlanThread;

        public void AbortTestPlan()
        {
            if (Plan.IsRunning)
            {
                testPlanThread.Abort();
                runAction.Title = "Run Test Plan";
                Update();
            }
        }
        
        public void RunTestPlan()
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
                    Application.MainLoop.Invoke(() => TestPlanFrame.Title = $"Test Plan - Running ");
                    Thread.Sleep(1000);
                    
                    for (int i = 0; i < 3 && PlanIsRunning; i++)
                    {
                        Application.MainLoop.Invoke(() => TestPlanFrame.Title += ">");
                        Thread.Sleep(1000);
                    }
                }
                
                Application.MainLoop.Invoke(() => TestPlanFrame.Title = "Test Plan");
            });
        }

        public override bool ProcessKey(KeyEvent kb)
        {
            if (Application.Current.MostFocused != this)
                return base.ProcessKey(kb);
            
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
                var index = SelectedItem;
                var steps = FlattenPlan();
                if (steps.Any())
                {
                    var step = steps[index];
                    step.Parent.ChildTestSteps.Remove(step);
                    Update();
                }

                return true;
            }
            if (kb.Key == Key.CursorRight && moveIndex > -1 && FlattenPlan()[SelectedItem].GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null)
            {
                injectStep = true;
                Update();
                return true;
            }
            if (kb.Key == Key.Space)
            {
                if (Plan.ChildTestSteps.Count == 0)
                    return base.ProcessKey(kb);
                
                if (moveIndex == -1)
                {
                    moveIndex = SelectedItem;
                    Update();
                    return true;
                }
                else
                {
                    var flatPlan = FlattenPlan();

                    var fromItem = flatPlan[moveIndex];
                    var toItem = flatPlan[SelectedItem];

                    var toIndex = toItem.Parent.ChildTestSteps.IndexOf(toItem);
                    var flatIndex = flatPlan.IndexOf(toItem);

                    if (IsParent(toItem, fromItem) == false)
                    {
                        fromItem.Parent.ChildTestSteps.Remove(fromItem);

                        if (injectStep)
                            toItem.ChildTestSteps.Add(fromItem);
                        else
                            toItem.Parent.ChildTestSteps.Insert(toIndex, fromItem);
                    }

                    injectStep = false;
                    moveIndex = -1;
                    Update();
                    SelectedItem = flatIndex;
                    return true;
                }
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
                var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "Add New Step");
                Application.Run(newStep);
                if (newStep.PluginType != null)
                {
                    AddNewStep(newStep.PluginType);
                }
                return true;
            }

            if (kb.IsShift && kb.Key == (Key.C|Key.CtrlMask) || kb.KeyValue == 'C')
            {
                // Copy
                var flatPlan = FlattenPlan();
                var copyStep = flatPlan[SelectedItem];
                var serializer = new TapSerializer();
                var xml = serializer.SerializeToString(copyStep);

                Clipboard.Contents = xml;
                
                return true;
            }

            if ((kb.IsShift && kb.Key == (Key.V|Key.CtrlMask) || kb.KeyValue == 'V') && Clipboard.Contents != null && SelectedItem > -1 )
            {
                // Paste
                var flatPlan = FlattenPlan();
                if (flatPlan.Count == 0)
                    return true;
                
                var toItem = flatPlan[SelectedItem];
                var toIndex = toItem.Parent.ChildTestSteps.IndexOf(toItem) + 1;
                var flatIndex = flatPlan.IndexOf(toItem);

                // Serialize Deserialize step to get a new instance
                var serializer = new TapSerializer();
                serializer.GetSerializer<TestStepSerializer>().AddKnownStepHeirarchy(Plan);
                var newStep = serializer.DeserializeFromString(Clipboard.Contents.ToString(), TypeData.FromType(typeof(TestPlan)), path: Plan.Path) as ITestStep;
                
                if (newStep != null)
                {
                    var existingStep = toItem.Parent.ChildTestSteps.ElementAtOrDefault(toIndex-1);
                    toItem.Parent.ChildTestSteps.Insert(toIndex, newStep);
                    Update();
                    var addedSteps = FlattenSteps(new[] {existingStep}).Count;
                    SelectedItem = flatIndex + addedSteps;
                }
                
                return true;
            }
            
            return base.ProcessKey(kb);
        }

        private bool IsParent(ITestStep step, ITestStep parent)
        {
            if (step == parent)
                return true;

            if (step.Parent != null && step.Parent is ITestStep)
                return IsParent(step.Parent as ITestStep, parent);

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenTap;
using OpenTAP.TUI.PropEditProviders;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class TestPlanView : ListView
    {
        private int moveIndex = -1;
        private bool injectStep = false;
        public TestPlan Plan { get; set; } = new TestPlan();

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
        private List<ITestStep> FlattenPlan()
        {
            List<ITestStep> _FlattenSteps(TestStepList steps)
            {
                var list = new List<ITestStep>();
                foreach (var item in steps)
                {
                    list.Add(item);
                    if (item.ChildTestSteps.Any())
                        list.AddRange(_FlattenSteps(item.ChildTestSteps));
                }
                return list;
            }

            return _FlattenSteps(Plan.ChildTestSteps);
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
        }

        public void Update()
        {
            var index = SelectedItem;
            SetSource(ExpandItems());
            if (Source.Count > 0)
                SelectedItem = (index > Source.Count - 1 ? Source.Count - 1 : index);
        }
        public void LoadTestPlan()
        {
            var dialog = new OpenDialog("Open a TestPlan", "Open");
            dialog.SelectionChanged += fileDialog =>
            {
                var path = Path.Combine(fileDialog.DirectoryPath.ToString(), fileDialog.FilePath.ToString());
                if (path != null)
                {
                    try
                    {
                        Plan = TestPlan.Load(path);
                        Update();
                    }
                    catch
                    {
                        TUI.Log.Info($"Could not load test plan '{path}'.");
                    }
                }
            };
            
            Application.Run(dialog);
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
                var dialog = new SaveDialog("Save TestPlan", "Where do you want to save the TestPlan?"){ NameFieldLabel = "Save: " };
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
                Plan.ChildTestSteps.Add(type.CreateInstance() as ITestStep);
                Update();
            } catch(Exception ex)
            {
                TUI.Log.Error(ex);
            }
        }
        public void InsertNewStep(ITypeData type)
        {
            var flatplan = FlattenPlan();
            if (flatplan.Count == 0)
            {
                AddNewStep(type);
                return;
            }

            var step = flatplan[SelectedItem];
            var index = step.Parent.ChildTestSteps.IndexOf(step);
            var flatIndex = flatplan.IndexOf(step);

            step.Parent.ChildTestSteps.Insert(index, type.CreateInstance() as ITestStep);
            Update();
            SelectedItem = flatIndex;
        }

        public override bool ProcessKey(KeyEvent kb)
        {
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
            }
            if (kb.Key == Key.CursorRight && moveIndex > -1 && FlattenPlan()[SelectedItem].GetType().GetCustomAttribute<AllowAnyChildAttribute>() != null)
            {
                injectStep = true;
                Update();
            }
            if (kb.Key == Key.CursorUp || kb.Key == Key.CursorDown)
            {
                injectStep = false;
                Update();
            }
            if (kb.Key == Key.Space)
            {
                if (moveIndex == -1)
                {
                    moveIndex = SelectedItem;
                    Update();
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
                }
            }

            if (kb.Key == Key.CursorRight || kb.Key == Key.CursorLeft)
                return true;

            if (kb.Key == Key.ControlS)
                SaveTestPlan(Plan.Path);
            
            if (kb.KeyValue == 115) // CTRL+Shift+S
                SaveTestPlan(null);
            
            if (kb.Key == Key.ControlO)
                LoadTestPlan();

            if (kb.Key == Key.ControlT)
            {
                var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "Add New Step");
                Application.Run(newStep);
                if (newStep.PluginType != null)
                {
                    InsertNewStep(newStep.PluginType);
                    Update();
                }
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

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
            var top = TopItem;
            SetSource(ExpandItems());
            if (top > 0)
                TopItem = top;
            if (Source.Count > 0)
                SelectedItem = (index > Source.Count - 1 ? Source.Count - 1 : index);
        }
        public void LoadTestPlan()
        {
            var dialog = new OpenDialog("Open a TestPlan", "Open");
            Application.Run(dialog);
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
                if (Plan.ChildTestSteps.Count == 0)
                    return base.ProcessKey(kb);
                
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
            {
                SaveTestPlan(kb.IsShift ? null : Plan.Path);
                return true;
            }

            if (kb.Key == Key.ControlO)
            {
                LoadTestPlan();
                return true;
            }

            if (kb.Key == Key.ControlT)
            {
                var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "Add New Step");
                Application.Run(newStep);
                if (newStep.PluginType != null)
                {
                    AddNewStep(newStep.PluginType);
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

using NStack;
using OpenTap;
using OpenTap.Cli;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class NewStepView : Window
    {
        private ReadOnlyCollection<Type> Plugins { get; set; }
        private ListView listView { get; set; }
        public Type Step { get; set; }

        public NewStepView() : base("Add New Step", 1)
        {
            Plugins = PluginManager.GetPlugins<ITestStep>();
            listView = new ListView(Plugins);
            Add(listView);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
            {
                Running = false;
                return true;
            }

            if (keyEvent.Key == Key.Enter)
            {
                var index = listView.SelectedItem;
                Step = Plugins[index];
                Running = false;
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }

    public class StepSettingView : Window
    {
        public object Value { get; set; }
        private TextField textField { get; set; }
        private object Input { get; set; }

        public StepSettingView(PropertyInfo prop, object input) : base(prop.Name, 1)
        {
            Input = input;
            textField = new TextField(input.ToString());
            Add(textField);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
            {
                Running = false;
                return true;
            }

            if (keyEvent.Key == Key.Enter)
            {
                try
                {
                    Value = TypeDescriptor.GetConverter(Input).ConvertFrom(textField.Text.ToString());
                }
                catch {}
                Running = false;
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }

    public class StepSettingsView : View
    {
        public ITestStep Step { get; set; }
        public List<PropertyInfo> StepProperties { get; set; }
        public ListView listView { get; set; } = new ListView();

        public StepSettingsView()
        {
            listView.CanFocus = true;
            Add(listView);
        }

        public void LoadSetting(ITestStep step)
        {
            Step = step;
            StepProperties = step.GetType().GetProperties().Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false && p.SetMethod?.IsPublic == true && p.GetCustomAttribute<XmlIgnoreAttribute>() == null).ToList();

            listView.SetSource(StepProperties.Select(p => $"{p.Name}: {p.GetValue(Step)?.ToString()}").ToList());
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter)
            {
                var index = listView.SelectedItem;
                var prop = StepProperties[index];
                var setting = new StepSettingView(prop, prop.GetValue(Step));
                Application.Run(setting);
                if (setting.Value != null)
                    prop.SetValue(Step, setting.Value);

                listView.SetSource(StepProperties.Select(p => $"{p.Name}: {p.GetValue(Step)?.ToString()}").ToList());
            }

            if (keyEvent.Key == Key.CursorRight)
                return true;

            return base.ProcessKey(keyEvent);
        }
    }

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
                return FlattenPlan()[SelectedItem];
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
            Application.Run(dialog);

            var path = dialog.FilePaths.FirstOrDefault();
            if (path != null)
            {
                Plan = TestPlan.Load(path);
                Update();
            }
        }
        public void NewTestPlan()
        {
            Plan = new TestPlan();
            Update();
        }
        public void SaveTestPlan(string path)
        {
            Plan.Save(path ?? Plan.Path);
        }
        public void AddNewStep(Type type)
        {
            Plan.ChildTestSteps.Add(Activator.CreateInstance(type) as ITestStep);
            Update();
        }
        public void InsertNewStep(Type type)
        {
            var flatplan = FlattenPlan();
            var step = flatplan[SelectedItem];
            var index = step.Parent.ChildTestSteps.IndexOf(step);
            var flatIndex = flatplan.IndexOf(step);

            step.Parent.ChildTestSteps.Insert(index, Activator.CreateInstance(type) as ITestStep);
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

            if (kb.Key == Key.CursorRight)
                return true;

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

    public class MainWindow : Window
    {
        public MainWindow(string title) : base(title)
        {
            
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.ControlX)
                Environment.Exit(0);

            return base.ProcessKey(keyEvent);
        }
    }

    [Display("tui")]
    public class TUI : ICliAction
    {
        [UnnamedCommandLineArgument("plan")]
        public string path { get; set; }

        public TestPlanView TestPlanView { get; set; } = new TestPlanView();
        public StepSettingsView StepSettingsView { get; set; } = new StepSettingsView();

        public int Execute(CancellationToken cancellationToken)
        {
            Application.Init();
            var top = Application.Top;

            var menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem("_File", new MenuItem [] {
                    new MenuItem("_New", "", TestPlanView.NewTestPlan),
                    new MenuItem("_Open", "", TestPlanView.LoadTestPlan),
                    new MenuItem("_Save", "", () => 
                    {
                        if (TestPlanView.Plan.Path != null)
                            TestPlanView.SaveTestPlan(null);
                        else
                        {
                            var dialog = new SaveDialog("Save TestPlan", "Where do you want to save the TestPlan?"){ NameFieldLabel = "Save: " };
                            Application.Run(dialog);
                            if (dialog.FileName != null)
                                TestPlanView.SaveTestPlan(Path.Combine(dialog.DirectoryPath.ToString(), dialog.FilePath.ToString()));
                        }
                    }),
                    new MenuItem("_Save As", "", () =>
                    {
                        var dialog = new SaveDialog("Save TestPlan", "Where do you want to save the TestPlan?"){ NameFieldLabel = "Save: " };
                        Application.Run(dialog);
                        if (dialog.FileName != null)
                            TestPlanView.SaveTestPlan(Path.Combine(dialog.DirectoryPath.ToString(), dialog.FilePath.ToString()));
                    }),
                    new MenuItem("_Quit", "", () => top.Running = false)
                }),
                new MenuBarItem("_Edit", new MenuItem [] {
                    new MenuItem("_Add New Step", "", () =>
                    {
                        var newStep = new NewStepView();
                        Application.Run(newStep);
                        if (newStep.Step != null)
                            TestPlanView.AddNewStep(newStep.Step);
                    }),
                    new MenuItem("_Insert New Step", "", () =>
                    {
                        var newStep = new NewStepView();
                        Application.Run(newStep);
                        if (newStep.Step != null)
                            TestPlanView.InsertNewStep(newStep.Step);
                    })
                })
            });
            top.Add(menu);

            var win = new MainWindow("OpenTAP TUI")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(win);

            var frame = new FrameView("TestPlan"){
                Width = Dim.Percent(75),
                Height = Dim.Fill()
            };
            frame.Add(TestPlanView);
            win.Add(frame);

            var frame2 = new FrameView("Settings")
            {
                X = Pos.Percent(75),
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            frame2.Add(StepSettingsView);
            win.Add(frame2);

            // Update step settings
            TestPlanView.SelectedChanged += () => { StepSettingsView.LoadSetting(TestPlanView.SelectedStep); };

            // Load plan from args
            if (path != null)
            {
                TestPlanView.Plan = TestPlan.Load(path);
                TestPlanView.Update();
                StepSettingsView.LoadSetting(TestPlanView.SelectedStep);
            }

            // Run application
            Application.Run();

            return 0;
        }
    
        public static void Main(string[] args)
        {
            new TUI(){ path = args.FirstOrDefault() }.Execute(new CancellationToken());
        }
    }
}

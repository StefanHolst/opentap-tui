using NStack;
using OpenTap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
                Value = TypeDescriptor.GetConverter(Input).ConvertFrom(textField.Text.ToString());
                Running = false;
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }

    public class StepSettingsView : Window
    {
        public ITestStep Step { get; set; }
        public List<PropertyInfo> StepProperties { get; set; }
        public ListView listView { get; set; }

        public StepSettingsView(ITestStep step) : base("Step Settings", 1)
        {
            Step = step;
            StepProperties = step.GetType().GetProperties().Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false && p.SetMethod?.IsPublic == true && p.GetCustomAttribute<XmlIgnoreAttribute>() == null).ToList();

            listView = new ListView(StepProperties.Select(p => $"{p.Name}: {p.GetValue(Step)?.ToString()}").ToList());
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
                var prop = StepProperties[index];
                var setting = new StepSettingView(prop, prop.GetValue(Step));
                Application.Run(setting);
                if (setting.Value != null)
                    prop.SetValue(Step, setting.Value);

                listView.SetSource(StepProperties.Select(p => $"{p.Name}: {p.GetValue(Step)?.ToString()}").ToList());
            }

            return base.ProcessKey(keyEvent);
        }
    }

    public class TestPlanView : ListView
    {
        private int moveIndex = -1;
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
            {
                allsteps[moveIndex] += " *";
            }

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

        public TestPlanView()
        {
            CanFocus = true;
        }

        public void Update()
        {
            Source = new ListWrapper(ExpandItems());
        }
        public void LoadTestPlan()
        {
            var dialog = new OpenDialog("Open a TestPlan", "Open");
            Application.Run(dialog);

            var path = dialog.FilePaths.FirstOrDefault();
            Plan = TestPlan.Load(path);
            Update();
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
            var index = SelectedItem;
            Plan.ChildTestSteps.Add(Activator.CreateInstance(type) as ITestStep);
            Update();
            SelectedItem = index;
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
            if (kb.Key == Key.Enter)
            {
                var index = SelectedItem;
                var step = FlattenPlan()[SelectedItem];
                var settings = new StepSettingsView(step);
                Application.Run(settings);
                Update();
                SelectedItem = index;
            }
            if (kb.Key == Key.DeleteChar)
            {
                var index = SelectedItem;
                var steps = FlattenPlan();
                var step = steps[index];
                step.Parent.ChildTestSteps.Remove(step);
                Update();
                steps = FlattenPlan();
                if (steps.Count > 0)
                    SelectedItem = index > steps.Count - 1 ? steps.Count - 1 : index;
            }
            if (kb.Key == Key.Space)
            {
                if (moveIndex == -1)
                {
                    moveIndex = SelectedItem;
                    Update();
                    SelectedItem = moveIndex;
                }
                else
                {
                    var flatPlan = FlattenPlan();

                    var fromItem = flatPlan[moveIndex];
                    var toItem = flatPlan[SelectedItem];
                    
                    var toIndex = toItem.Parent.ChildTestSteps.IndexOf(toItem);
                    var flatIndex = flatPlan.IndexOf(toItem);

                    fromItem.Parent.ChildTestSteps.Remove(fromItem);

                    toItem.Parent.ChildTestSteps.Insert(toIndex, fromItem);

                    moveIndex = -1;
                    Update();
                    SelectedItem = flatIndex;
                }
            }

            return base.ProcessKey(kb);
        }

        class ListWrapper : IListDataSource
        {
            IList src;
            BitArray marks;
            int count;

            public ListWrapper(IList source)
            {
                count = source.Count;
                marks = new BitArray(count);
                this.src = source;
            }

            public int Count => src.Count;

            void RenderUstr(ConsoleDriver driver, ustring ustr, int col, int line, int width)
            {
                int byteLen = ustr.Length;
                int used = 0;
                for (int i = 0; i < byteLen;)
                {
                    (var rune, var size) = Utf8.DecodeRune(ustr, i, i - byteLen);
                    var count = Rune.ColumnWidth(rune);
                    if (used + count >= width)
                        break;
                    driver.AddRune(rune);
                    used += count;
                    i += size;
                }
                for (; used < width; used++)
                {
                    driver.AddRune(' ');
                }
            }

            public void Render(ListView container, ConsoleDriver driver, bool marked, int item, int col, int line, int width)
            {
                container.Move(col, line);
                var t = src[item];
                if (t is ustring)
                {
                    RenderUstr(driver, (ustring)t, col, line, width);
                }
                else if (t is string)
                {
                    RenderUstr(driver, (string)t, col, line, width);
                }
                else
                    RenderUstr(driver, t.ToString(), col, line, width);
            }

            public bool IsMarked(int item)
            {
                if (item >= 0 && item < count)
                    return marks[item];
                return false;
            }

            public void SetMark(int item, bool value)
            {
                if (item >= 0 && item < count)
                    marks[item] = value;
            }
        }
    }

    [Display("tui")]
    public class TUI
    {
        public static TestPlanView TestPlanView { get; set; } = new TestPlanView();

        public static void Main(string[] args)
        {
            Application.Init();
            var top = Application.Top;

            var win = new Window("OpenTAP TUI")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(win);

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
                        TestPlanView.AddNewStep(newStep.Step);
                    }),
                    new MenuItem("_Insert New Step", "", () =>
                    {
                        var newStep = new NewStepView();
                        Application.Run(newStep);
                        TestPlanView.InsertNewStep(newStep.Step);
                    })
                })
            });
            top.Add(menu);

            win.Add(TestPlanView);

            if (args.Any())
            {
                TestPlanView.Plan = TestPlan.Load(args[0]);
                TestPlanView.Update();
            }
            Application.Run();
        }
    }
}

using NStack;
using OpenTap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
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

        public StepSettingsView(ITestStep step) : base ("Step Settings", 1)
        {
            Step = step;
            StepProperties = step.GetType().GetProperties().Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false).ToList();

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

            return _ExpandItem(Plan.ChildTestSteps);
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
            
            Plan = TestPlan.Load(@"C:\Users\Stefan\Documents\Visual Studio 2017\Projects\OpenTAP.TUI\OpenTAP.TUI\Untitled.TapPlan");
            Update();
        }

        private void Update()
        {
            Source = new ListWrapper(ExpandItems());
        }

        public TestPlan Plan { get; set; } = new TestPlan();

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

        public void AddNewStep(Type type)
        {
            var step = FlattenPlan()[SelectedItem];
            var index = step.Parent.ChildTestSteps.IndexOf(step);

            step.Parent.ChildTestSteps.Insert(index, Activator.CreateInstance(type) as ITestStep);
            Update();
        }

        public override bool ProcessKey(KeyEvent kb)
        {
            if (kb.Key == Key.Enter)
            {
                var step = FlattenPlan()[SelectedItem];
                var settings = new StepSettingsView(step);
                Application.Run(settings);
                Update();
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
                    SelectedItem = index > steps.Count - 1 ? steps.Count-1 : index;
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

            // Creates the top-level window to show
            var win = new Window("OpenTAP TUI")
            {
                X = 0,
                Y = 1, // Leave one row for the toplevel menu

                // By using Dim.Fill(), it will automatically resize without manual intervention
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(win);

            // Creates a menubar, the item "New" has a help menu.
            var menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_New", "", TestPlanView.NewTestPlan),
                    new MenuItem ("_Open", "", TestPlanView.LoadTestPlan),
                    new MenuItem ("_Quit", "", () => top.Running = false)
                }),
                new MenuBarItem ("_Edit", new MenuItem [] {
                    new MenuItem ("_Add New Step", "", () => 
                    {
                        var newStep = new NewStepView();
                        Application.Run(newStep);
                        TestPlanView.AddNewStep(newStep.Step);
                    })
                })
            });
            top.Add(menu);
            
            win.Add(TestPlanView);
            Application.Run();
        }
    }
}

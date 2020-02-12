using OpenTap;
using OpenTap.Cli;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using OpenTap.TUI;
using Terminal.Gui;
using TraceSource = OpenTap.TraceSource;

namespace OpenTAP.TUI
{
    public class MainWindow : Window
    {
        public View StepSettingsView { get; set; }
        public View TestPlanView { get; set; }
        public View LogFrame { get; set; }

        public MainWindow(string title) : base(title)
        {

        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter && MostFocused is TestPlanView)
            {
                FocusNext();
                return true;
            }

            if (keyEvent.Key == Key.ControlX || keyEvent.Key == Key.ControlC || (keyEvent.Key == Key.Esc && MostFocused is TestPlanView))
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                {
                    Application.RequestStop();
                    TUI.Quitting = true;
                }
            }

            if (keyEvent.Key == Key.Tab || keyEvent.Key == Key.BackTab)
            {
                if (TestPlanView.HasFocus)
                    StepSettingsView.FocusFirst();
                else
                    TestPlanView.FocusFirst();

                return true;
            }

            if (keyEvent.Key == Key.F1)
            {
                TestPlanView.FocusFirst();
                return true;
            }
            if (keyEvent.Key == Key.F2)
            {
                StepSettingsView.FocusFirst();
                return true;
            }
            if (keyEvent.Key == Key.F3)
            {
                var kevent = keyEvent;
                kevent.Key = Key.F2;
                StepSettingsView.ProcessKey(kevent);
                return true;
            }
            if (keyEvent.Key == Key.F4)
            {
                LogFrame.FocusFirst();
                return true;
            }

            if (keyEvent.Key == Key.Esc && MostFocused is TestPlanView == false)
            {
                FocusPrev();
            }

            return base.ProcessKey(keyEvent);
        }
    }

    [Display("tui")]
    public class TUI : ICliAction
    {
        [UnnamedCommandLineArgument("plan")]
        public string path { get; set; }

        public static TraceSource Log = OpenTap.Log.CreateSource("TUI");
        public static bool Quitting { get; set; }

        public TestPlanView TestPlanView { get; set; }
        public PropertiesView StepSettingsView { get; set; }
        public FrameView LogFrame { get; set; }

        public int Execute(CancellationToken cancellationToken)
        {
            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += (s, e) =>
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                {
                    Application.RequestStop();
                    e.Cancel = true;
                    Quitting = true;
                }
            };

            try
            {
                Application.Init();
                var top = Application.Top;

                TestPlanView = new TestPlanView();
                StepSettingsView = new PropertiesView();
                var settings = TypeData.GetDerivedTypes<ComponentSettings>()
                    .Where(x => x.CanCreateInstance && (x.GetAttribute<BrowsableAttribute>()?.Browsable ?? true));
                Dictionary<MenuItem, string> groupItems = new Dictionary<MenuItem, string>();
                foreach (var setting in settings.OfType<TypeData>())
                {
                    ComponentSettings obj = null;
                    try
                    {
                        obj = ComponentSettings.GetCurrent(setting.Load());
                        if(obj == null) continue;
                    }catch
                    {
                        continue;
                    }

                    var setgroup = setting.GetAttribute<SettingsGroupAttribute>()?.GroupName ?? "Settings";
                    var name = setting.GetDisplayAttribute().Name;
                    Toplevel settingsView;
                    if (setting.DescendsTo(TypeData.FromType(typeof(ConnectionSettings))))
                    {
                        settingsView = new ConnectionSettingsWindow(name);
                    }else
                    if (setting.DescendsTo(TypeData.FromType(typeof(ComponentSettingsList<,>))))
                    {
                        settingsView = new ResourceSettingsWindow(name,(IList)obj);
                    }
                    else
                    {
                        settingsView = new ComponentSettingsWindow(obj);
                    }

                    var menuItem = new MenuItem("_" + name, "", () =>
                    {
                        Application.Run(settingsView);
                    });
                    groupItems[menuItem] = setgroup;
                }
                
                
                var filemenu = new MenuBarItem("_File", new MenuItem[]
                {
                    new MenuItem("_New", "", () =>
                    {
                        TestPlanView.NewTestPlan();
                        StepSettingsView.LoadProperties(null);
                    }),
                    new MenuItem("_Open", "", TestPlanView.LoadTestPlan),
                    new MenuItem("_Save", "", () => { TestPlanView.SaveTestPlan(TestPlanView.Plan.Path); }),
                    new MenuItem("_Save As", "", () => { TestPlanView.SaveTestPlan(null); }),
                    new MenuItem("_Quit", "", () => Application.RequestStop())
                });
                var editmenu = new MenuBarItem("_Edit", new MenuItem[]
                {
                    new MenuItem("_Add New Step", "", () =>
                    {
                        var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "Add New Step");
                        Application.Run(newStep);
                        if (newStep.PluginType != null)
                        {
                            TestPlanView.AddNewStep(newStep.PluginType);
                            StepSettingsView.LoadProperties(TestPlanView.SelectedStep);
                        }
                    }),
                    new MenuItem("_Insert As Child", "", () =>
                    {
                        var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "Insert As Child");
                        Application.Run(newStep);
                        if (newStep.PluginType != null)
                        {
                            TestPlanView.InsertNewStep(newStep.PluginType);
                            StepSettingsView.LoadProperties(TestPlanView.SelectedStep);
                        }
                    })
                });

                var helpmenu = new MenuBarItem("_Help", new MenuItem[]
                {
                    new MenuItem("_Help", "", () =>
                    {
                        var helpWin = new HelpWindow();
                        Application.Run(helpWin);
                    })
                });
                
                List<MenuBarItem> menuBars = new List<MenuBarItem>();
                menuBars.Add(filemenu);
                menuBars.Add(editmenu);
                foreach (var group in groupItems.GroupBy(x => x.Value))
                {
                    var m = new MenuBarItem("_" + group.Key,
                        group.OrderBy(x => x.Key.Title).Select(x => x.Key).ToArray()
                    );
                    menuBars.Add(m);
                }
                menuBars.Add(helpmenu);
                
                var menu = new MenuBar(menuBars.ToArray());
                menu.Closing += (s, e) => 
                {
                    TestPlanView.FocusFirst();
                };
                top.Add(menu);

                var win = new MainWindow("OpenTAP TUI")
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(),
                    StepSettingsView = StepSettingsView,
                    TestPlanView = TestPlanView
                };
                top.Add(win);

                var testPlanFrame = new FrameView("Test Plan")
                {
                    Width = Dim.Percent(75),
                    Height = Dim.Percent(75)
                };
                testPlanFrame.Add(TestPlanView);
                win.Add(testPlanFrame);

                var settingsFrame = new FrameView("Settings")
                {
                    X = Pos.Percent(75),
                    Width = Dim.Fill(),
                    Height = Dim.Percent(75)
                };
                settingsFrame.Add(StepSettingsView);
                win.Add(settingsFrame);

                LogFrame = new FrameView("Log Panel")
                {
                    Y = Pos.Percent(75),
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };
                LogFrame.Add(new LogPanelView());
                win.Add(LogFrame);
                win.LogFrame = LogFrame;

                // Update step settings
                TestPlanView.SelectedChanged += () => { StepSettingsView.LoadProperties(TestPlanView.SelectedStep); };
                
                // Update testplanview
                StepSettingsView.PropertiesChanged += TestPlanView.Update;
                
                // Stop OpenTAP from taking over the terminal for user inputs.
                UserInput.SetInterface(null);
                
                // Load plan from args
                if (path != null)
                {
                    try
                    {
                        if (File.Exists(path) == false)
                        {
                            // file does not exist, lets just create it.
                            var plan = new TestPlan();
                            plan.Save(path);
                        }

                        TestPlanView.Plan = TestPlan.Load(path);
                        TestPlanView.Update();
                        StepSettingsView.LoadProperties(TestPlanView.SelectedStep);
                    }
                    catch
                    {
                        Log.Warning("Unable to load plan {0}.", path);
                    }
                }
                // Run application
                Application.Run();
            }
            catch (Exception ex)
            {
                Log.Error(DefaultExceptionMessages.DefaultExceptionMessage);
                Log.Debug(ex);
                
                if (Quitting == false)
                    Execute(cancellationToken);
            }

            return 0;
        }
    }
}

using OpenTap;
using OpenTap.Cli;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTap.Tui.Views;
using OpenTap.Tui.Windows;
using Terminal.Gui;
using TraceSource = OpenTap.TraceSource;

namespace OpenTap.Tui
{
    public class MainWindow : Window
    {
        public View StepSettingsView { get; set; }
        public TestPlanView TestPlanView { get; set; }
        public View LogFrame { get; set; }

        public MainWindow(string title) : base(title)
        {
            
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == (Key.CtrlMask|Key.ShiftMask|Key.Enter))
            {
                if (MessageBox.Query("Focus Mode", "Do you want to enter focus mode?", "Yes", "No") == 0)
                {
                    var someWin = new SomthingWindow();
                    Application.Run(someWin);
                }
            }

            if (keyEvent.Key == Key.Enter && MostFocused is TestPlanView)
            {
                FocusNext();
                return true;
            }

            if (keyEvent.Key == Key.ControlX || keyEvent.Key == Key.ControlC || (keyEvent.Key == Key.Esc && MostFocused is TestPlanView))
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                {
                    Application.Shutdown();
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
            
            if (TestPlanView.PlanIsRunning == false && keyEvent.Key == Key.F5)
            {
                // Start the testplan
                TestPlanView.RunTestPlan();
                return true;
            }

            if (TestPlanView.PlanIsRunning && keyEvent.Key == Key.F5 && keyEvent.IsShift)
            {
                // Abort plan?
                if (MessageBox.Query(50, 7, "Abort Test Plan", "Are you sure you want to abort the test plan?", "Yes", "No") == 0)
                    TestPlanView.AbortTestPlan();
            }

            if (keyEvent.Key == Key.ControlS)
                return TestPlanView.ProcessKey(keyEvent);

            return base.ProcessKey(keyEvent);
        }
    }

    [Display("tui")]
    public class TUI : ICliAction
    {
        [UnnamedCommandLineArgument("plan")]
        public string path { get; set; }

        public static TraceSource Log = OpenTap.Log.CreateSource("TUI");
        public TestPlanView TestPlanView { get; set; }
        public PropertiesView StepSettingsView { get; set; }
        public FrameView LogFrame { get; set; }

        public static Toplevel Top { get; set; }
        
        /// <summary> When the test plan is selected this is used to not automatically
        /// switch back to the test step in the properties view.</summary>
        public bool TestPlanSelected { get; set; }

        /// <summary>  Marks which thread is the main thread. (needed for user input request)</summary>
        public static TapThread MainThread;
        

        public int Execute(CancellationToken cancellationToken)
        {
            MainThread = TapThread.Current;
            cancellationToken.Register(() =>
            {
                Application.Shutdown();
            });

            // Remove console listener to stop any log messages being printed on top of the TUI
            var consoleListener = OpenTap.Log.GetListeners().OfType<ConsoleTraceListener>().FirstOrDefault();
            if (consoleListener != null)
                OpenTap.Log.RemoveListener(consoleListener);

            // Stop OpenTAP from taking over the terminal for user inputs.
            UserInput.SetInterface(null);
            
            // Add tui user input
            UserInput.SetInterface(new TuiUserInput());
            
            try
            {
                Application.Init();
                Top = Application.Top;
                TuiSettings.Current.LoadSettings();

                TestPlanView = new TestPlanView();
                StepSettingsView = new PropertiesView();

                // menu items
                var runmenuItem = new MenuItem("_Run Test Plan", "", () =>
                {
                    if (TestPlanView.PlanIsRunning)
                        TestPlanView.AbortTestPlan();
                    else
                        TestPlanView.RunTestPlan();
                });
                
                var editTestPlanSettings = new MenuItem("_Test Plan Settings", "", () =>
                {
                    TestPlanSelected = !TestPlanSelected;
                    loadSelected();
                    StepSettingsView.FocusFirst();
                });
                
                var filemenu = new MenuBarItem("_File", new MenuItem[]
                {
                    new MenuItem("_New", "", () =>
                    {
                        TestPlanView.NewTestPlan();
                        TestPlanSelected = true;
                        loadSelected();
                    }),
                    new MenuItem("_Open", "", TestPlanView.LoadTestPlan),
                    new MenuItem("_Save", "", () => { TestPlanView.SaveTestPlan(TestPlanView.Plan.Path); }),
                    new MenuItem("Save _As", "", () => { TestPlanView.SaveTestPlan(null); }),
                    new MenuItem("_Quit", "", () => Application.Shutdown())
                });
                var editmenu = new MenuBarItem("_Edit", new MenuItem[]
                {
                    new MenuItem("_Insert New Step", "", () =>
                    {
                        var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "New Step");
                        Application.Run(newStep);
                        if (newStep.PluginType != null)
                        {
                            TestPlanView.AddNewStep(newStep.PluginType);
                            loadSelected();
                        }
                    }),
                    new MenuItem("Insert New Step _Child", "", () =>
                    {
                        var newStep = new NewPluginWindow(TypeData.FromType(typeof(ITestStep)), "New Step Child");
                        Application.Run(newStep);
                        if (newStep.PluginType != null)
                        {
                            TestPlanView.InsertNewChildStep(newStep.PluginType);
                            loadSelected();
                        }
                    }),
                    runmenuItem,
                    editTestPlanSettings
                });
                var helpmenu = new MenuBarItem("_Help", new MenuItem[]
                {
                    new MenuItem("_Help", "", () =>
                    {
                        var helpWin = new HelpWindow();
                        Application.Run(helpWin);
                    }),
                    new MenuItem("_Focus Mode", "", () =>
                    {
                        var someWin = new SomthingWindow();
                        Application.Run(someWin);
                    })
                });
                
                // Settings menu
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

                        var setgroup = setting.GetAttribute<SettingsGroupAttribute>()?.GroupName ?? "Settings";
                        var name = setting.GetDisplayAttribute().Name;

                        var menuItem = new MenuItem("_" + name, "", () =>
                        {
                            Window settingsView;
                            if (setting.DescendsTo(TypeData.FromType(typeof(ConnectionSettings))))
                            {
                                settingsView = new ConnectionSettingsWindow(name);
                            }
                            else if (setting.DescendsTo(TypeData.FromType(typeof(ComponentSettingsList<,>))))
                            {
                                settingsView = new ResourceSettingsWindow(name,(IList)obj);
                            }
                            else
                            {
                                settingsView = new ComponentSettingsWindow(obj);
                            }
                            Application.Run(settingsView);
                        });
                        groupItems[menuItem] = setgroup;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
                
                // Create list of all menu items, used in menu bar
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
                
                // Add menu bar
                var menu = new MenuBar(menuBars.ToArray());
                
                var initEditMenuItemCount = editmenu.Children.Length;
                void updateEditMenuItems()
                {
                    editmenu.Children = editmenu.Children.Take(initEditMenuItemCount)
                        .Concat(StepSettingsView.ActiveMenuItems).ToArray();
                }

                StepSettingsView.SelectionChanged += updateEditMenuItems;
                
                menu.MenuClosing += () => 
                {
                    if(!TestPlanSelected)
                        TestPlanView.FocusFirst();
                };
                Top.Add(menu);

                // Create main window and add it to top item of application
                var win = new MainWindow("OpenTAP TUI")
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(),
                    StepSettingsView = StepSettingsView,
                    TestPlanView = TestPlanView
                };
                Top.Add(win);

                // Add testplan view
                var testPlanFrame = new FrameView("Test Plan")
                {
                    Width = Dim.Percent(75),
                    Height = Dim.Percent(75)
                };
                testPlanFrame.Add(TestPlanView);
                TestPlanView.Frame = testPlanFrame;
                win.Add(testPlanFrame);

                TestPlanView.TestPlanStarted = () =>
                {
                    runmenuItem.Title = "_Abort Test Plan";
                };
                TestPlanView.TestPlanStopped = () =>
                {
                    runmenuItem.Title = "_Run Test Plan";
                };

                // Add step settings view
                var settingsFrame = new FrameView("Settings")
                {
                    X = Pos.Percent(75),
                    Width = Dim.Fill(),
                    Height = Dim.Percent(75)
                };
                settingsFrame.Add(StepSettingsView);
                win.Add(settingsFrame);

                // Add log panel
                LogFrame = new FrameView("Log Panel")
                {
                    Y = Pos.Percent(75),
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };
                LogFrame.Add(new LogPanelView());
                win.Add(LogFrame);
                win.LogFrame = LogFrame;

                // Update StepSettingsView when TestPlanView changes selected step
                TestPlanView.SelectedItemChanged += args =>
                {
                    TestPlanSelected = false;
                    loadSelected();
                };
                
                // Update testplanview when step settings are changed
                StepSettingsView.PropertiesChanged += () =>
                {
                    if(TestPlanSelected == false)
                        TestPlanView.Update();
                };
                
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
                        loadSelected();
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
            }

            return 0;
        }

        void loadSelected()
        {
            if (TestPlanSelected)
                StepSettingsView.LoadProperties(TestPlanView.Plan);
            else
                StepSettingsView.LoadProperties(TestPlanView.SelectedStep);
        }
    }
}

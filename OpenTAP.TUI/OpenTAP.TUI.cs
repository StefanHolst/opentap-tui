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

namespace OpenTap.Tui
{
    public class MainWindow : Window
    {
        public PropertiesView StepSettingsView { get; set; }
        public TestPlanView TestPlanView { get; set; }
        public View LogFrame { get; set; }
        public static HelperButtons helperButtons { get; private set; }

        public MainWindow(string title) : base(title)
        {
            Modal = true;
            
            helperButtons = new HelperButtons
            {
                Width = Dim.Fill(),
                Height = 1
            };
            
            Initialized += (s, e) =>
            {
                helperButtons.Y = Pos.Bottom(LogFrame);
                Add(helperButtons);
            };
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter && MostFocused is TestPlanView && this.IsTopActive())
            {
                FocusNext();
                return true;
            }

            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Close))
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                    Application.RequestStop();
                return true;
            }

            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.SwapView))
            {
                if (TestPlanView.HasFocus)
                    StepSettingsView.FocusFirst();
                else
                    TestPlanView.SetFocus();
            
                return true;
            }

            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.FocusTestPlan))
            {
                TestPlanView.SetFocus();
                return true;
            }
            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.FocusStepSettings))
            {
                StepSettingsView.FocusFirst();
                return true;
            }
            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.FocusDescription))
            {
                StepSettingsView.FocusLast();
                return true;
            }
            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.FocusLog))
            {
                LogFrame.SetFocus();
                return true;
            }
            
            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Save))
                return TestPlanView.ProcessKey(keyEvent);

            if (helperButtons.ProcessKey(keyEvent))
                return true;
            
            return base.ProcessKey(keyEvent);
        }
    }

    [Display("tui", "View, edit and run test plans using TUI.")]
    public class TUI : TuiAction
    {
        [UnnamedCommandLineArgument("plan")]
        public string path { get; set; }

        public TestPlanView TestPlanView { get; set; }
        public PropertiesView StepSettingsView { get; set; }
        public FrameView LogFrame { get; set; }

        public override int TuiExecute(CancellationToken cancellationToken)
        {
            var gridWidth = TuiSettings.Current.TestPlanGridWidth;
            var gridHeight = TuiSettings.Current.TestPlanGridHeight;
            TestPlanView = new TestPlanView()
            {
                Y = 1,
                Width = Dim.Percent(gridWidth),
                Height = Dim.Percent(gridHeight)
            };
            StepSettingsView = new PropertiesView(true);
            
            var filemenu = new MenuBarItem("_File", new MenuItem[]
            {
                new MenuItem("_New", "", () =>
                {
                    TestPlanView.NewTestPlan();
                    StepSettingsView.LoadProperties(null);
                }),
                new MenuItem("_Open", "", TestPlanView.LoadTestPlan),
                new MenuItem("_Save", "", () => { TestPlanView.SaveTestPlan(TestPlanView.Plan.Path); }),
                new MenuItem("Save _As", "", () => { TestPlanView.SaveTestPlan(null); }),
                new MenuItem("_Quit", "", () => Application.RequestStop())
            });
            var toolsmenu = new MenuBarItem("_Tools", new MenuItem[]
            {
                new MenuItem("_Results Viewer (Experimental)", "", () =>
                {
                    var reswin = new ResultsViewerWindow()
                    {
                        Width = Dim.Fill(),
                        Height = Dim.Fill(),
                    };
            
                    // Run application
                    Application.Run(reswin);
                    TestPlanView.Update(); // make sure the helperbuttons have been refreshed
                }),
                new MenuItem("_Package Manager (Experimental)", "", () =>
                {
                    var pmwin = new PackageManagerWindow()
                    {
                        Width = Dim.Fill(),
                        Height = Dim.Fill(),
                    };
            
                    // Run application
                    Application.Run(pmwin);
                    TestPlanView.Update(); // make sure the helperbuttons have been refreshed
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

            var settingsProfile = new MenuItem("Profiles", "", () =>
            {
                var profileWindow = new SettingsProfileWindow("Bench");
                Application.Run(profileWindow);
            });
            groupItems[settingsProfile] = "Bench";
            
            // Create list of all menu items, used in menu bar
            List<MenuBarItem> menuBars = new List<MenuBarItem>();
            menuBars.Add(filemenu);
            foreach (var group in groupItems.GroupBy(x => x.Value))
            {
                var m = new MenuBarItem("_" + group.Key,
                    group.OrderBy(x => x.Key.Title).Select(x => x.Key).ToArray()
                );
                menuBars.Add(m);
            }
            menuBars.Add(toolsmenu);
            menuBars.Add(helpmenu);
            
            // Create main window and add it to top item of application
            var win = new MainWindow("OpenTAP TUI")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                StepSettingsView = StepSettingsView,
                TestPlanView = TestPlanView
            };
            
            // Add menu bar
            var menu = new MenuBar(menuBars.ToArray());
            win.Add(menu);

            // Add testplan view
            win.Add(TestPlanView);

            // Add step settings view
            string settingsName = $"[ {KeyMapHelper.GetKeyName(KeyTypes.FocusStepSettings)} Settings ]";
            var settingsFrame = new FrameView(settingsName)
            {
                X = Pos.Right(TestPlanView),
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Height(TestPlanView)
            };
            StepSettingsView.TreeViewFilterChanged += (filter) => { settingsFrame.Title = string.IsNullOrEmpty(filter) ? settingsName : $"{settingsName} - {filter}"; };
            settingsFrame.Add(StepSettingsView);
            win.Add(settingsFrame);

            // Add log panel
            LogFrame = new FrameView($"[ {KeyMapHelper.GetKeyName(KeyTypes.FocusLog)} Log Panel ]")
            {
                Y = Pos.Bottom(TestPlanView),
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            LogFrame.Add(new LogPanelView());
            win.Add(LogFrame);
            win.LogFrame = LogFrame;

            // Resize grid elements when TUI settings are changed
            TuiSettings.Current.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "Size")
                {
                    var s = TuiSettings.Current;
                    TestPlanView.Width = Dim.Percent(s.TestPlanGridWidth);
                    TestPlanView.Height = Dim.Percent(s.TestPlanGridHeight);
                }
            };

            // Update StepSettingsView when TestPlanView changes selected step
            TestPlanView.SelectionChanged += args =>
            {
                if (args is TestPlan)
                {
                    StepSettingsView.LoadProperties(TestPlanView.Plan);
                    StepSettingsView.FocusFirst();
                }
                else
                    StepSettingsView.LoadProperties(args);
            };
            
            // Update testplanview when step settings are changed
            StepSettingsView.PropertiesChanged += () =>
            {
                TestPlanView.Update(true);
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

                    TestPlanView.LoadTestPlan(path);
                }
                catch
                {
                    Log.Warning("Unable to load plan {0}.", path);
                }
            }

            // Run application
            Application.Run(win);

            return 0;
        }
    }
}

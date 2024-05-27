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
    public class MainWindow : BaseWindow
    {
        internal static MainWindow Current;

        public PropertiesView StepSettingsView { get; set; }
        public TestPlanView TestPlanView { get; set; }
        public View LogFrame { get; set; }
        static HelperButtons helperButtons { get; set; }

        public static event Action UnsavedChangesCreated;
        private static bool _containsUnsavedChanges;
        public static bool ContainsUnsavedChanges
        {
            get => _containsUnsavedChanges;
            set
            {
                _containsUnsavedChanges = value;
                UnsavedChangesCreated?.Invoke();
            }
        }
        public MenuBarItem FileMenu { get; set; }

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

        public override bool OnKeyUp(KeyEvent keyEvent)
        {
            return keyEvent.Key.HasFlag(Key.AltMask) ? true : base.OnKeyUp(keyEvent);
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
                if (ContainsUnsavedChanges)
                {
                    switch (MessageBox.Query(50, 7, "Unsaved changes!", "Do you want to save before exiting?", "Save", "Don't save", "Cancel"))
                    {
                        case 0:
                            // Save.
                            TestPlanView.SaveTestPlan(TestPlanView.Plan.Path);
                            break;
                        case 1:
                            // Don't save.
                            break;
                        case 2:
                        // Cancel.
                        default:
                            return false;
                    }
                }
                else if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") != 0)
                {
                    return false;
                }
                else if (TestPlanView.Plan.IsRunning && MessageBox.Query(50, 7, "Are you sure?", "A test plan is currently running, are you sure you want to exit?", "Exit", "Cancel") == 1)
                {
                    return false;
                }
                Application.RequestStop();
                return true;
            }

            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.SwapView) || KeyMapHelper.IsKey(keyEvent, KeyTypes.SwapViewBack))
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
            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Help))
            {
                var helpWin = new HelpWindow();
                Application.Run(helpWin);
                return true;
            }

            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Save))
                return TestPlanView.ProcessKey(keyEvent);

            if (helperButtons.ProcessKey(keyEvent))
                return true;
            
            return base.ProcessKey(keyEvent);
        }
        public static void SetActions(List<MenuItem> list, View owner)
        {
            helperButtons.SetActions(list, owner);
            Current.FileMenu.Children = list.ToArray();
        }
    }

    [Display("tui", "View, edit and run test plans using TUI.")]
    public class TUI : TuiAction
    {
        [UnnamedCommandLineArgument("plan")]
        public string path { get; set; }

        [CommandLineArgument("focus", Description = "If true will open the tui in focus mode.")]
        public bool focusMode { get; set; } = false;

        public TestPlanView TestPlanView { get; set; }
        public PropertiesView StepSettingsView { get; set; }
        public FrameView LogFrame { get; set; }

        private bool TryGetBufferHeight(out int bh)
        {
            try
            {
                bh = Console.BufferHeight;
                return true;
            }
            catch
            {
                bh = 0;
                return false;
            }
        }

        private void SetBufferHeight(int h)
        {
            try
            {
                Console.BufferHeight = h;
            }
            catch
            {
                // ignore
            }
        }

        public override int TuiExecute(CancellationToken cancellationToken)
        {
            bool canGetHeight = TryGetBufferHeight(out var bufferHeight);

            var gridWidth = TuiSettings.Current.TestPlanGridWidth;
            var gridHeight = TuiSettings.Current.TestPlanGridHeight;
            TestPlanView = new TestPlanView()
            {
                Y = 1,
                Width = Dim.Percent(gridWidth),
                Height = Dim.Percent(gridHeight)
            };
            StepSettingsView = new PropertiesView(true);
            StepSettingsView.IsReadOnly = () => TestPlanView.Plan.Locked;

            var filemenu = new MenuBarItem("File", new MenuItem[]
            {
                new MenuItem("New", "", () =>
                {
                    if (TestPlanView.SaveOrDiscard())
                    {
                        TestPlanView.NewTestPlan();
                        StepSettingsView.LoadProperties(null);
                    }
                }),
                new MenuItem("Open", "", () => 
                {
                    if (TestPlanView.SaveOrDiscard())
                        TestPlanView.LoadTestPlan();
                }),
                new MenuItem("Save", "", () => { TestPlanView.SaveTestPlan(TestPlanView.Plan.Path); }),
                new MenuItem("Save As", "", () => { TestPlanView.SaveTestPlan(null); }),
                new MenuItem("Quit", "", () => 
                {
                    if (TestPlanView.SaveOrDiscard()) 
                        Application.RequestStop();
                }),
            });
            var editMenu = new MenuBarItem("Edit", new MenuItem[]
            {
                new MenuItem("New", "", () =>
                {
                    TestPlanView.NewTestPlan();
                    StepSettingsView.LoadProperties(null);
                }),
            });
            var toolsmenu = new MenuBarItem("Tools", new MenuItem[]
            {
                new MenuItem("Results Viewer (Experimental)", "", () =>
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
                new MenuItem("Package Manager (Experimental)", "", () =>
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

            var helpmenu = new MenuBarItem(KeyMapHelper.GetKeyName(KeyTypes.Help, "Help"), new MenuItem[]
            {
                new MenuItem("Help", "", () =>
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
                    if (obj == null) continue;

                    var setgroup = setting.GetAttribute<SettingsGroupAttribute>()?.GroupName ?? "Settings";
                    var name = setting.GetDisplayAttribute().Name;

                    var menuItem = new MenuItem(name, "", () =>
                    {
                        Window settingsView;
                        if (setting.DescendsTo(TypeData.FromType(typeof(ConnectionSettings))))
                        {
                            settingsView = new ConnectionSettingsWindow(name);
                        }
                        else if (setting.DescendsTo(TypeData.FromType(typeof(ComponentSettingsList<,>))))
                        {
                            settingsView = new ResourceSettingsWindow(name, (IList)obj);
                        }
                        else
                        {
                            settingsView = new ComponentSettingsWindow(obj);
                        }
                        Application.Run(settingsView);
                        TestPlanView.UpdateHelperButtons();
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
            menuBars.Add(editMenu);
            foreach (var group in groupItems.GroupBy(x => x.Value))
            {
                var m = new MenuBarItem(group.Key,
                    group.OrderBy(x => x.Key.Title).Select(x => x.Key).ToArray()
                );
                menuBars.Add(m);
            }
            menuBars.Add(toolsmenu);
            menuBars.Add(helpmenu);
            var menuLabel = new Label($"[ {KeyMapHelper.GetKeyName(KeyTypes.FocusMenu)} ]")
            {
                ColorScheme = Colors.Menu,
            };
            var menu = new MenuBar(menuBars.ToArray()) {
                Shortcut = KeyMapHelper.GetShortcutKey(KeyTypes.FocusMenu),
                X = Pos.Right(menuLabel),
            };

            // Create main window and add it to top item of application
            var win = new MainWindow("OpenTAP TUI")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                StepSettingsView = StepSettingsView,
                TestPlanView = TestPlanView,
                FileMenu = editMenu
            };
            MainWindow.Current = win;

            // Add menu bar
            win.Add(menuLabel);
            win.Add(menu);

            // Add testplan view
            win.Add(TestPlanView);

            // Add step settings view
            string settingsName = KeyMapHelper.GetKeyName(KeyTypes.FocusStepSettings, "Settings");
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
            LogFrame = new FrameView(KeyMapHelper.GetKeyName(KeyTypes.FocusLog, "Log Panel"))
            {
                Y = Pos.Bottom(TestPlanView),
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            LogPanelView logPanelView = new LogPanelView();
            LogFrame.Add(logPanelView);
            TestPlanView.RunStarted += () => {
                if (TuiSettings.Current.ClearOnRun) logPanelView.ClearLog();
            };
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

            Application.IsMouseDisabled = true;
            if (focusMode)
                Application.MainLoop.Invoke(() => FocusMode.StartFocusMode(FocusModeUnlocks.Command, false));

            // Run application
            Application.Run(win);

            Application.Shutdown();
            TestPlanView.RemoveRecoveryfile();
            if (canGetHeight)
                SetBufferHeight(bufferHeight);

            return 0;
        }
    }
}

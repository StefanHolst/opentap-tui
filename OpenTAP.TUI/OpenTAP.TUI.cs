using NStack;
using OpenTap;
using OpenTap.Cli;
using OpenTap.Diagnostic;
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

            if (keyEvent.Key == Key.ControlX || keyEvent.Key == Key.ControlC)
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                    Application.RequestStop();
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

        public int Execute(CancellationToken cancellationToken)
        {
            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += (s, e) =>
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                {
                    Application.RequestStop();
                    e.Cancel = true;
                }
            };

            try
            {
                Application.Init();
                var top = Application.Top;

                TestPlanView = new TestPlanView();
                StepSettingsView = new PropertiesView();

                var menu = new MenuBar(new MenuBarItem[] {
                    new MenuBarItem("_File", new MenuItem [] {
                        new MenuItem("_New", "", () => 
                        {
                            TestPlanView.NewTestPlan();
                            StepSettingsView.LoadProperties(null);
                        }),
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
                        new MenuItem("_Quit", "", () => Application.RequestStop())
                    }),
                    new MenuBarItem("_Edit", new MenuItem [] {
                        new MenuItem("_Add New Step", "", () =>
                        {
                            var newStep = new NewPluginWindow(typeof(ITestStep), "Add New Step");
                            Application.Run(newStep);
                            if (newStep.PluginType != null)
                            {
                                TestPlanView.AddNewStep(newStep.PluginType);
                                StepSettingsView.LoadProperties(TestPlanView.SelectedStep);
                            }
                        }),
                        new MenuItem("_Insert New Step", "", () =>
                        {
                            var newStep = new NewPluginWindow(typeof(ITestStep), "Insert New Step");
                            Application.Run(newStep);
                            if (newStep.PluginType != null)
                            {
                                TestPlanView.InsertNewStep(newStep.PluginType);
                                StepSettingsView.LoadProperties(TestPlanView.SelectedStep);
                            }
                        })
                    }),
                    new MenuBarItem("_Settings", new MenuItem[]{
                        new MenuItem("Engine", "", () => {
                            var settingsView = new ComponentSettingsWindow(EngineSettings.Current);
                            Application.Run(settingsView);
                        })
                    }),
                    new MenuBarItem("_Resources", new MenuItem[]{
                        new MenuItem("_DUTs", "", () =>
                        {
                            var settingsView = new ResourceSettingsWindow<IDut>("DUTs");
                            Application.Run(settingsView);
                        }),
                        new MenuItem("_Instruments", "", () =>
                        {
                            var settingsView = new ResourceSettingsWindow<IInstrument>("Instruments");
                            Application.Run(settingsView);
                        }),
                        new MenuItem("_Result Listeners", "", () =>
                        {
                            var settingsView = new ResourceSettingsWindow<IResultListener>("Result Listeners");
                            Application.Run(settingsView);
                        })
                    }),
                    new MenuBarItem("_Help", new MenuItem[]{
                        new MenuItem("_Help", "", () => {
                            var helpWin = new HelpWindow();
                            Application.Run(helpWin);
                        })
                    })
                });
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

                // Load plan from args
                if (path != null)
                {
                    TestPlanView.Plan = TestPlan.Load(path);
                    TestPlanView.Update();
                    StepSettingsView.LoadProperties(TestPlanView.SelectedStep);
                }

                // Run application
                Application.Run();
            }
            catch (Exception ex)
            {
                Log.Error("Something went wrong in the TUI.");
                Log.Debug(ex);
                Execute(cancellationToken);
            }

            return 0;
        }
    }
}

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

        public int Execute(CancellationToken cancellationToken)
        {
            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += (s, e) =>
            {
                Environment.Exit(0);
                e.Cancel = true;
            };

            while (true)
            {
                try
                {
                    Application.Init();
                    var top = Application.Top;

                    TestPlanView = new TestPlanView();
                    StepSettingsView = new PropertiesView();

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
                            new MenuItem("_Instrumentss", "", () =>
                            {
                                var settingsView = new ResourceSettingsWindow<IInstrument>("Instruments");
                                Application.Run(settingsView);
                            }),
                            new MenuItem("_Result Listeners", "", () =>
                            {
                                var settingsView = new ResourceSettingsWindow<IResultListener>("Result Listeners");
                                Application.Run(settingsView);
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


                    var logFrame = new FrameView("Log Panel")
                    {
                        Y = Pos.Percent(75),
                        Width = Dim.Fill(),
                        Height = Dim.Fill()
                    };
                    logFrame.Add(new LogPanelView());
                    win.Add(logFrame);

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
                    Console.WriteLine(ex);
                }
            }
            return 0;
        }

        public static void Main(string[] args)
        {
            new TUI() { path = args.FirstOrDefault() }.Execute(new CancellationToken());
        }
    }
}

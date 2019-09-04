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
        public PropertiesView StepSettingsView { get; set; } = new PropertiesView();

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
                        var newStep = new NewPluginView(typeof(ITestStep), "Add New Step");
                        Application.Run(newStep);
                        if (newStep.PluginType != null)
                        {
                            TestPlanView.AddNewStep(newStep.PluginType);
                            StepSettingsView.LoadProperties(TestPlanView.SelectedStep);
                        }
                    }),
                    new MenuItem("_Insert New Step", "", () =>
                    {
                        var newStep = new NewPluginView(typeof(ITestStep), "Insert New Step");
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
                        var settingsView = new ComponentSettingsView(EngineSettings.Current);
                        Application.Run(settingsView);
                    }),
                    new MenuItem("Results", "", () => {})
                }),
                new MenuBarItem("_Resources", new MenuItem[]{
                    new MenuItem("_DUTs", "", () =>
                    {
                        var settingsView = new ResourceSettingsView<IDut>("DUTs");
                        Application.Run(settingsView);
                    }),
                    new MenuItem("_Instrumentss", "", () =>
                    {
                        var settingsView = new ResourceSettingsView<IInstrument>("Instruments");
                        Application.Run(settingsView);
                    }),
                    new MenuItem("_Result Listeners", "", () =>
                    {
                        var settingsView = new ResourceSettingsView<IResultListener>("Result Listeners");
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

            var frame = new FrameView("TestPlan")
            {
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

            return 0;
        }

        public static void Main(string[] args)
        {
            new TUI() { path = args.FirstOrDefault() }.Execute(new CancellationToken());
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using OpenTap.Package;
using Terminal.Gui;
using OpenTap.Tui.Views;

namespace OpenTap.Tui.Windows
{
    public class PackageManagerWindow : EditWindow
    {
        private TreeView treeView { get; set; }
        private PackageDetailsView detailsView { get; set; }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (TuiAction.CurrentAction is TuiPm && (keyEvent.Key == Key.ControlX || keyEvent.Key == Key.ControlC || keyEvent.Key == Key.Esc))
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                {
                    Application.Shutdown();
                }
            }
            
            return base.ProcessKey(keyEvent);
        }
        
        public PackageManagerWindow() : base("Package Manager")
        {
            // Add settings menu
            var setting = TypeData.FromType(typeof(PackageManagerSettings));
            var obj = ComponentSettings.GetCurrent(setting.Load());
            var name = setting.GetDisplayAttribute().Name;
            var menu = new MenuBar(new []
            {
                new MenuBarItem("Settings", new []
                {
                    new MenuItem("Settings", name, () =>
                    {
                        var settingsView = new ComponentSettingsWindow(obj);
                        Application.Run(settingsView);
                    })
                })
            });
            
            // Package Details
            var detailsFrame = new FrameView("Package Details")
            {
                X = Pos.Percent(33),
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Percent(75) - 1
            };
            detailsView = new PackageDetailsView();
            detailsFrame.Add(detailsView);
            
            // Log panel
            var logsFrame = new FrameView("Log Panel")
            {
                Y = Pos.Bottom(detailsFrame),
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            logsFrame.Add(new LogPanelView(Application.Top));
            
            // Packages
            var packageFrame = new FrameView("Packages")
            {
                Y = 1,
                Width = Dim.Percent(33),
                Height = Dim.Percent(75) - 1
            };
            var packageList = new PackageListView();
            packageList.SelectionChanged += () =>
            {
                detailsView.LoadPackage(packageList.SelectedPackage, packageList.installation, packageList.installedOpentap);
            };
            detailsView.LoadPackage(packageList.SelectedPackage,packageList.installation, packageList.installedOpentap);
            packageFrame.Add(packageList);
            
            Add(menu);
            Add(packageFrame);
            Add(detailsFrame);
            Add(logsFrame);

            // Load packages in parallel
            bool running = true;
            Task.Run(() =>
            {
                packageList.LoadPackages();
                running = false;
            });
            Task.Run(() =>
            {
                while (running)
                {
                    Application.MainLoop.Invoke(() => packageFrame.Title = $"Packages ");
                    Thread.Sleep(100);
                    
                    for (int i = 0; i < 3 && running; i++)
                    {
                        Application.MainLoop.Invoke(() => packageFrame.Title += ".");
                        Thread.Sleep(100);
                    }
                }
                Application.MainLoop.Invoke(() => packageFrame.Title = $"Packages");
            });
        }
    }
}
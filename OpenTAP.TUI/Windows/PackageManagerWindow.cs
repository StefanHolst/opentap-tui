using System;
using System.Threading;
using System.Threading.Tasks;
using OpenTap.Package;
using Terminal.Gui;
using OpenTap.Tui.Views;

namespace OpenTap.Tui.Windows
{
    public class PackageManagerWindow : Window
    {
        private PackageDetailsView detailsView { get; set; }
        private readonly FrameView packageFrame;
        private readonly PackageListView packageList;
        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (TuiAction.CurrentAction is TuiPm && KeyMapHelper.IsKey(keyEvent, KeyTypes.Close))
            {
                if (MessageBox.Query(50, 7, "Quit?", "Are you sure you want to quit?", "Yes", "No") == 0)
                {
                    Application.MainLoop.Invoke(() => Application.RequestStop());
                }

                return true;
            }
            else if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Cancel))
            {
                var handled = base.ProcessKey(keyEvent);
                if (handled) return true;
                Application.RequestStop();
                return true;
            }
            
            return base.ProcessKey(keyEvent);
        }

        /// <summary> Reloads the packages asynchronously. </summary>
        public Task LoadPackages()
        {
            bool running = true;
            Task.Run(() =>
            {
                try
                {
                    packageList.LoadPackages();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    running = false;
                }
            });
            return Task.Run(() =>
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
        
        public PackageManagerWindow() : base("Package Manager")
        {
            Modal = true;
            
            // Add settings menu
            var setting = TypeData.FromType(typeof(PackageManagerSettings));
            var obj = ComponentSettings.GetCurrent(setting.Load());
            var name = setting.GetDisplayAttribute().Name;
            var menu = new MenuBar(new []
            {
                new MenuBarItem("Settings", new []
                {
                    new MenuItem(name, "Settings", () =>
                    {
                        var settingsView = new ComponentSettingsWindow(obj);
                        Application.Run(settingsView);
                    }),
                    new MenuItem("Refresh", "", () => LoadPackages())
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
            packageFrame = new FrameView("Packages")
            {
                Y = 1,
                Width = Dim.Percent(33),
                Height = Dim.Percent(75) - 1
            };
            packageList = new PackageListView();
            packageList.SelectionChanged += () => { detailsView.LoadPackage(packageList.SelectedPackage, packageList.installation, packageList.installedOpentap); };
            packageList.TreeViewFilterChanged += (filter) => { packageFrame.Title = string.IsNullOrEmpty(filter) ? "Packages" : $"Packages - {filter}"; }; 

            detailsView.LoadPackage(packageList.SelectedPackage,packageList.installation, packageList.installedOpentap);
            packageFrame.Add(packageList);
            
            Add(menu);
            Add(packageFrame);
            Add(detailsFrame);
            Add(logsFrame);

            // Load packages in parallel
            LoadPackages();
        }
    }
}
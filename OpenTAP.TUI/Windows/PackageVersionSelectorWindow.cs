using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using OpenTap.Package;
using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class PackageVersionSelectorWindow : Window
    {
        private ListView versionsView { get; set; }
        private PackageDetailsView detailsView { get; set; }
        private Button installButton { get; set; }
        
        private PackageViewModel package;
        private Installation installation;
        private PackageDef installedOpentap;
        private List<PackageViewModel> versions;
        private TapThread runningThread;
        
        public PackageVersionSelectorWindow(PackageViewModel package, Installation installation, PackageDef installedOpentap) : base("Install Package")
        {
            this.package = package;
            this.installation = installation;
            this.installedOpentap = installedOpentap;
            
            // Install button
            installButton = new Button("", true)
            {
                X = Pos.Center()
            };
            installButton.Clicked += () => { InstallButtonClicked(false); };
            
            // Get package versions
            versions = GetVersions(package);
            versionsView = new ListView()
            {
                AllowsMarking = true
            };
            versionsView.SelectedItemChanged += args =>
            {
                detailsView.LoadPackage(versions[args.Item], installation, installedOpentap);
                installButton.Text = versions[args.Item].isInstalled ? "Uninstall" : "Install";
            };
            UpdateVersions();
            
            // Versions frame
            var versionsFrame = new FrameView("Versions")
            {
                Width = Dim.Percent(33),
                Height = Dim.Percent(75) - 3
            };
            versionsFrame.Add(versionsView);
            
            // Details frame
            var detailsFrame = new FrameView("Package Details")
            {
                X = Pos.Percent(33),
                Width = Dim.Fill(),
                Height = Dim.Percent(75)
            };
            detailsView = new PackageDetailsView();
            detailsFrame.Add(detailsView);

            // Install frame
            var installFrame = new FrameView("")
            {
                Height = 3,
                Width = Dim.Percent(33),
                Y = Pos.Bottom(versionsFrame)
            };
            installFrame.Add(installButton);
            
            // Log panel
            var logFrame = new FrameView("Log Panel")
            {
                Y = Pos.Percent(75),
                Height = Dim.Fill(),
                Width = Dim.Fill()
            };
            logFrame.Add(new LogPanelView(this));
            
            // Add frames
            Add(versionsFrame);
            Add(detailsFrame);
            Add(installFrame);
            Add(logFrame);
        }

        void InstallButtonClicked(bool force)
        {
            var selectedPackage = versions.FirstOrDefault();
            for (int i = 0; i < versionsView.Source.Count; i++)
            {
                if (versionsView.Source.IsMarked(i))
                    selectedPackage = versions[i];
            }

            if (runningThread != null)
            {
                // cancel
                runningThread.Abort();
                return;
            }
            
            if (selectedPackage.isInstalled == false)
            {
                var installAction = new PackageInstallAction()
                {
                    Packages = new[] {package.Name},
                    Version = selectedPackage.Version.ToString(),
                    Force = force
                };
                installAction.Error += exception =>
                {
                    // TODO: Fix message width, wrap the text..
                    Application.MainLoop.Invoke(() => MessageBox.ErrorQuery(50, 7, "Installation Error", exception.Message, "Ok"));
                };
                
                installButton.Text = "Cancel";
                runningThread = TapThread.Start(() =>
                {
                    // Add tui user input
                    UserInput.SetInterface(new TuiUserInput());
                    
                    installAction.Execute(TapThread.Current.AbortToken);
                    Application.MainLoop.Invoke(UpdateVersions);
                    runningThread = null;
                });
            }
            else
            {
                var uninstallAction = new PackageUninstallAction()
                {
                    Packages = new []{ package.Name },
                    Force = force
                };
                
                installButton.Text = "Cancel";
                runningThread = TapThread.Start(() =>
                {
                    // Add tui user input
                    UserInput.SetInterface(new TuiUserInput());
                    
                    uninstallAction.Execute(TapThread.Current.AbortToken);
                    Application.MainLoop.Invoke(UpdateVersions);
                    runningThread = null;
                });
            }        
        }

        List<PackageViewModel> GetVersions(PackageViewModel package)
        {
            HttpClient hc = new HttpClient();
            hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var content = new StringContent(@"query Query {
                packages(" + $"name: \"{package.Name}\", os: \"{installedOpentap.OS}\", architecture: \"{installedOpentap.Architecture}\"" + @") {
                    architecture
                    oS
                    version
                    owner
                    sourceUrl
                    description
                    dependencies{
                        name
                        version
                    }
                }
            }");
            var response = hc.PostAsync("http://packages.opentap.io/3.1/Query", content).Result;
            var jsonData = response.Content.ReadAsStringAsync().Result;
            
            // Remove unicode chars
            jsonData = Regex.Replace(jsonData, @"[^\u0000-\u007F]+", string.Empty);
            
            // Parse the json response data
            var installedPackage = installation.GetPackages().FirstOrDefault(p => p.Name == package.Name);
            var list = new List<PackageViewModel>();
            var jsonPackages = (JsonElement)JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData)["packages"];
            foreach (var item in jsonPackages.EnumerateArray())
            {
                var version = JsonSerializer.Deserialize<PackageViewModel>(item.GetRawText());
                version.Name = package.Name;

                if (installedPackage != null && installedPackage.Version == version.Version)
                    version.isInstalled = true;
                
                list.Add(version);
            }

            return list;
        }

        void UpdateVersions()
        {
            var installedPackage = installation.GetPackages().FirstOrDefault(p => p.Name == package.Name);
            versionsView.SetSource(versions.Select(p => $"{p.Version}{(installedPackage?.Version == p.Version ? " (Installed)" : "")}").ToList());
            versionsView.Source.SetMark(0, true);
            
            installButton.Text = versions.FirstOrDefault()?.isInstalled == true ? "Uninstall" : "Install";
        }
        
        public override bool ProcessKey (KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Space)
            {
                for (int i = 0; i < versionsView.Source.Count; i++)
                    versionsView.Source.SetMark(i, false);
            }
            
            if (keyEvent.Key == Key.Esc)
            {
                Running = false;
                return true;
            }

            if (keyEvent.Key == Key.Enter)
            {
                // ShowVersions();
            }
            
            return base.ProcessKey (keyEvent);
        }
    }
}
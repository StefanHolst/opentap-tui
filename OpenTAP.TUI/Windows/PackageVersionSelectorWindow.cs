using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenTap.Package;
using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class PackageVersionSelectorWindow : BaseWindow
    {
        private ListView versionsView { get; set; }
        private PackageDetailsView detailsView { get; set; }
        private Button installButton { get; set; }
        
        private PackageViewModel package;
        private Installation installation;
        private PackageDef installedVersion;
        private PackageDef installedOpentap;
        private List<PackageViewModel> versions;
        private TapThread runningThread;
        
        public PackageVersionSelectorWindow(PackageViewModel package, Installation installation, PackageDef installedOpentap) : base("Install Package")
        {
            Modal = true;
            this.package = package;
            this.installation = installation;
            installedVersion = installation.GetPackages().FirstOrDefault(p => p.Name == package.Name);
            this.installedOpentap = installedOpentap;
            
            // Install button
            installButton = new Button("", true)
            {
                X = Pos.Center()
            };
            installButton.Clicked += () => { InstallButtonClicked(false); };
            
            // Get package versions
            // versions = GetVersions(package);
            versionsView = new ListView()
            {
                AllowsMarking = true,
                AllowsMultipleSelection = false
            };
            versionsView.SelectedItemChanged += args =>
            {
                detailsView.LoadPackage(versions?[args.Item], installation, installedOpentap);
            };
            versionsView.MarkUnmarkChanged += (marked, item) =>
            {
                installButton.Text = marked && versions[item].Version == installedVersion?.Version ? "Uninstall" : "Install";
            };
            // UpdateVersions();
            
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
            
            // Load packages in parallel
            var t1 = Task.Run(() =>
            {
                try
                {
                    versions = GetVersions();
                    UpdateVersions();
                }
                catch (Exception ex)
                {
                    var log = Log.CreateSource("Package Manager");
                    log.Error($"Error getting package versions: '{ex.Message}'"); 
                    log.Debug(ex);
                }
            });
            Task.Run(() =>
            {
                while (!t1.IsCompleted)
                {
                    Application.MainLoop.Invoke(() => versionsFrame.Title = $"Versions ");
                    t1.Wait(100);

                    for (int i = 0; i < 3 && !t1.IsCompleted; i++)
                    {
                        Application.MainLoop.Invoke(() => versionsFrame.Title += ".");
                        t1.Wait(100);
                    }
                }
                Application.MainLoop.Invoke(() =>
                {
                    versionsFrame.Title = $"Versions";

                    if (versions.Count == 0)
                    {  // this occurs if the architecture or OS does not match any of the packages.
                        MessageBox.Query("No Plugin Packages Available", "No compatible plugin packages available.",
                            "OK");
                        Application.RequestStop();
                    }
                });
            });
        }

        void InstallButtonClicked(bool force)
        {
            var selectedPackage = versions?.FirstOrDefault();
            if (selectedPackage == null)
                return;

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

            if ((installedVersion != null && installedVersion.Version == selectedPackage.Version) == false)
            {
                var installAction = new PackageInstallAction()
                {
                    Packages = new[] { package.Name },
                    Version = selectedPackage.Version.ToString(),
                    Force = force
                };
                installAction.Error += exception =>
                {
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
                    Packages = new[] { package.Name },
                    Force = force
                };

                uninstallAction.Error += exception =>
                {
                    Application.MainLoop.Invoke(() => MessageBox.ErrorQuery(50, 7, "Uninstallation Error", exception.Message, "Ok"));
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

        List<PackageViewModel> GetVersions()
        {
            var list = new List<PackageViewModel>();
            var semvers = new HashSet<SemanticVersion>();

            var repos = PackageManagerSettings.Current.Repositories.Where(r => r.IsEnabled).OrderByDescending(r => r.Manager is FilePackageRepository);
            foreach (var repository in repos)
            {
                List<PackageViewModel> _list = new List<PackageViewModel>();
                if (repository.Manager is HttpPackageRepository httpRepository)
                    _list = GetHttpPackages(httpRepository);
                else if (repository.Manager is FilePackageRepository fileRepository)
                    _list = GetFilePackages(fileRepository);

                foreach (var pm in _list)
                {
                    if (semvers.Contains(pm.Version) == false)
                    {
                        list.Add(pm);
                        semvers.Add(pm.Version);
                    }
                }
            }

            return list;
        }

        List<PackageViewModel> GetFilePackages(FilePackageRepository repository)
        {
            TuiPm.Log.Info("Loading packages from: " + repository.Url);
            var list = new List<PackageViewModel>();

            var versions = repository.GetPackageVersions(package.Name, TuiPm.CancellationToken, installedOpentap);
            foreach (var version in versions)
            {
                var packageDef = repository.GetPackages(new PackageSpecifier(package.Name, VersionSpecifier.Parse(version.Version.ToString())), TuiPm.CancellationToken).FirstOrDefault();
                if (packageDef != null)
                    list.Add(new PackageViewModel(packageDef));
            }

            return list;
        }

        List<PackageViewModel> GetHttpPackages(HttpPackageRepository repository)
        {
            var list = new List<PackageViewModel>();

            TuiPm.Log.Info("Loading packages from: " + repository.Url);
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
            var response = hc.PostAsync("http://packages.opentap.io/3.1/Query", content, TuiPm.CancellationToken).Result;
            var jsonData = response.Content.ReadAsStringAsync().Result;

            // Remove unicode chars
            jsonData = Regex.Replace(jsonData, @"[^\u0000-\u007F]+", string.Empty);

            // Parse the json response data
            var jsonPackages = (JsonElement)JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData)["packages"];
            foreach (var item in jsonPackages.EnumerateArray())
            {
                var version = JsonSerializer.Deserialize<PackageViewModel>(item.GetRawText());
                version.Name = package.Name;
                if (list.Contains(version) == false)
                    list.Add(version);
            }

            return list;
        }

        void UpdateVersions()
        {
            installedVersion = installation.GetPackages().FirstOrDefault(p => p.Name == package.Name);

            versionsView.SetSource(versions.Select(p => $"{p.Version}{(installedVersion?.Version == p.Version ? " (Installed)" : "")}").ToList());
            versionsView.Source.SetMark(0, true);
            versionsView.SelectedItem = 0;

            installButton.Text = versions.FirstOrDefault()?.Version == installedVersion?.Version ? "Uninstall" : "Install";
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Cancel))
            {
                Running = false;
                return true;
            }

            return base.ProcessKey(keyEvent);
        }
    }
}

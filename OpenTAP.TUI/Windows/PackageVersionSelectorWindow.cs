using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        private PackageDef installedOpentap;
        
        public PackageVersionSelectorWindow(PackageViewModel package, PackageDef installedOpentap) : base("Install Package")
        {
            this.package = package;
            this.installedOpentap = installedOpentap;
            
            installButton = new Button("", true)
            {
                X = Pos.Center()
            };
            
            // Get package versions
            var versions = GetVersions(package);
            versionsView = new ListView(versions.Select(p => $"{p.Version}{(p.isInstalled ? " (Installed)" : "")}").ToList())
            {
                AllowsMarking = true
            };
            versionsView.Source.SetMark(0, true);
            versionsView.SelectedItemChanged += args =>
            {
                detailsView.LoadPackage(versions[args.Item], installedOpentap);
                installButton.Text = versions[args.Item].isInstalled ? "Uninstall" : "Install";
            };
            
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


            var installFrame = new FrameView("")
            {
                Height = 3,
                Width = Dim.Percent(33),
                Y = Pos.Bottom(versionsFrame)
            };
            installFrame.Add(installButton);
            
            
            Add(versionsFrame);
            Add(detailsFrame);
            Add(installFrame);






            // // Force install checkbox
            // var forceCheck = new CheckBox("Force Install?") { Y = 5 };
            // Add(forceCheck);
            //
            // // Add install button
            // var installButton = new Button($"{(package.isInstalled ? "Uninstall" : "Install")} Package")
            // {
            //     Y = 12,
            //     X = Pos.Center()
            // };
            // installButton.Clicked += () =>
            // {
            //     InstallButtonClicked(forceCheck.Checked);
            // };
            // Add(installButton);
            //
            // // Log panel
            // var logFrame = new FrameView("Log Panel")
            // {
            //     Y = Pos.Percent(50),
            //     Height = Dim.Fill(),
            //     Width = Dim.Fill()
            // };
            // logFrame.Add(new LogPanelView());
            // Add(logFrame);
        }

        void InstallButtonClicked(bool force)
        {
                    
            if (package.isInstalled == false)
            {
                var installAction = new PackageInstallAction()
                {
                    Packages = new[] {package.Name},
                    Version = package.Version.ToString(),
                    Force = force
                };
                installAction.Error += exception =>
                {
                    // TODO: Fix message width, wrap the text..
                    MessageBox.ErrorQuery(50, 7, "Installation Error", exception.Message);
                };
                installAction.ProgressUpdate += (percent, message) =>
                {
                    // progress.Fraction = (float)(percent / 100.0);
                };
                TapThread.Start(() => { installAction.Execute(TapThread.Current.AbortToken); });
            }
            else
            {
                var uninstallAction = new PackageUninstallAction()
                {
                    Packages = new []{ package.Name },
                    Force = force
                };
                    
                TapThread.Start(() => { uninstallAction.Execute(TapThread.Current.AbortToken); });
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
            var list = new List<PackageViewModel>();
            var jsonPackages = (JsonElement)JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData)["packages"];
            foreach (var item in jsonPackages.EnumerateArray())
            {
                var version = JsonSerializer.Deserialize<PackageViewModel>(item.GetRawText());
                version.Name = package.Name;

                if (package.isInstalled && package.installedVersion == version.Version)
                    version.isInstalled = true;
                
                list.Add(version);
            }

            return list;
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
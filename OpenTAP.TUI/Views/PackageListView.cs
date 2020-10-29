using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using OpenTap.Package;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class PackageListView : View
    {
        private TreeView treeView { get; set; }
        public PackageDef installedOpenTap { get; set; }

        public event Action SelectionChanged;
        public PackageViewModel SelectedPackage { get; set; }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter && Application.Current is PackageVersionSelectorWindow == false)
            {
                var dialog = new PackageVersionSelectorWindow(SelectedPackage, installedOpenTap);
                Application.Run(dialog);
                
                // TODO: Update list, package might have been installed or uninstalled
                var packages = GetPackages();
                treeView.SetTreeViewSource(packages);
            }
            
            return base.ProcessKey(keyEvent);
        }

        public PackageListView()
        {
            var packages = GetPackages();
            
            treeView = new TreeView(
                (item) =>
                {
                    var package = item as PackageViewModel;
                    return $"{package.Name}{(package.isInstalled ? " (installed)" : "")}";
                },
                (item) => (item as PackageViewModel).Group?.Split(new []{'\\', '/'}, StringSplitOptions.RemoveEmptyEntries));
            treeView.SetTreeViewSource(packages);

            treeView.SelectedItemChanged += (_) =>
            {
                SelectedPackage = treeView.SelectedObject.obj as PackageViewModel;
                SelectionChanged?.Invoke();
            };
            SelectedPackage = treeView.SelectedObject.obj as PackageViewModel;
            SelectionChanged?.Invoke();
            
            Add(treeView);
        }

        List<PackageViewModel> GetPackages()
        {
            // Get packages from repo
            HttpClient hc = new HttpClient();
            hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var content = new StringContent(@"query Query {
                packages(distinctName: true) {
                    name
                    version
                    group
                    owner
                    sourceUrl
                    description
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
                list.Add(JsonSerializer.Deserialize<PackageViewModel>(item.GetRawText()));
            }
            
            // Get installed packages
            var installation = new Installation(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var installedPackages = installation.GetPackages();
            installedOpenTap = installation.GetOpenTapPackage();
            foreach (var installedPackage in installedPackages)
            {
                var existing = list.FirstOrDefault(p => p.Name == installedPackage.Name);
                if (existing != null)
                {
                    existing.isInstalled = true;
                    existing.installedVersion = installedPackage.Version;
                }
                else
                {
                    list.Add(new PackageViewModel()
                    {
                        Name = installedPackage.Name,
                        Description = installedPackage.Description,
                        Group = installedPackage.Group,
                        isInstalled = false,
                        Owner = installedPackage.Owner
                    });
                }
            }
            
            // Order packages
            list = list.OrderByDescending(p => p.isInstalled).ThenBy(p => p.Group + p.Name).ToList();
 
            return list;
        }
    }

    public class PackageViewModel : IPackageIdentifier
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("version")]
        [JsonConverter(typeof(SemanticVersionConverter))]
        public SemanticVersion Version { get; set; }
        
        [JsonPropertyName("architecture")]
        [JsonConverter(typeof(CpuArchitectureConverter))]
        public CpuArchitecture Architecture { get; set; }
        
        [JsonPropertyName("oS")]
        public string OS { get; set; }
        
        [JsonPropertyName("group")]
        public string Group { get; set; }
        
        [JsonPropertyName("owner")]
        public string Owner { get; set; }
        
        [JsonPropertyName("sourceUrl")]
        public string SourceUrl { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("dependencies")]
        [JsonConverter(typeof(PackageDependenciesConverter))]
        public List<PackageDependency> Dependencies { get; set; }
        
        public bool isInstalled { get; set; }
        public SemanticVersion installedVersion { get; set; }
    }

    public class CpuArchitectureConverter : JsonConverter<CpuArchitecture>
    {
        public override CpuArchitecture Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (Enum.TryParse(reader.GetString(), out CpuArchitecture architecture))
                return architecture;

            return CpuArchitecture.Unspecified;
        }

        public override void Write(Utf8JsonWriter writer, CpuArchitecture value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
    
    public class SemanticVersionConverter : JsonConverter<SemanticVersion>
    {
        public override SemanticVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (SemanticVersion.TryParse(reader.GetString(), out SemanticVersion version))
                return version;
            
            return null;
        }

        public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
    
    public class PackageDependenciesConverter : JsonConverter<List<PackageDependency>>
    {
        public override List<PackageDependency> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<PackageDependency>();
            
            JsonDocument.TryParseValue(ref reader, out var doc);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var obj = element.EnumerateObject();
                var name = obj.ElementAtOrDefault(0).Value.ToString();
                var version = obj.ElementAtOrDefault(1).Value.ToString();

                if (string.IsNullOrEmpty(name) == false && VersionSpecifier.TryParse(version, out var v))
                    list.Add(new PackageDependency(name, v));
            }

            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<PackageDependency> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
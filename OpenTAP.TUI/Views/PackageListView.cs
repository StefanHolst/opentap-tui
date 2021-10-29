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
using System.Threading;
using OpenTap.Package;
using OpenTap.Tui.Windows;
using Terminal.Gui;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OpenTap.Tui.Views
{
    public class PackageListView : View
    {
        private TreeView treeView { get; set; }
        public Installation installation { get; set; }
        public PackageDef installedOpentap { get; set; }
        public event Action SelectionChanged;
        public PackageViewModel SelectedPackage { get; set; }

        private List<PackageViewModel> packages;
        private List<PackageDef> installedPackages;
        
        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Enter && Application.Current is PackageVersionSelectorWindow == false)
            {
                var dialog = new PackageVersionSelectorWindow(SelectedPackage, installation, installedOpentap);
                Application.Run(dialog);
                
                installedPackages = installation.GetPackages();
                packages = packages.OrderByDescending(p => installedPackages.Any(i => i.Name == p.Name)).ThenBy(p => p.Group + p.Name).ToList();
                treeView.SetTreeViewSource(packages);

                return true;
            }
            
            return base.ProcessKey(keyEvent);
        }

        public PackageListView()
        {
            installation = new Installation(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            installedOpentap = installation.GetOpenTapPackage();
            installedPackages = installation.GetPackages();
            
            treeView = new TreeView(
                (item) =>
                {
                    var package = item as PackageViewModel;
                    return $"{package.Name}{(installedPackages.Any(p => p.Name == package.Name) ? " (installed)" : "")}";
                },
                (item) => (item as PackageViewModel)?.Group?.Split(new []{'\\', '/'}, StringSplitOptions.RemoveEmptyEntries));

            treeView.SelectedItemChanged += (_) =>
            {
                SelectedPackage = treeView.SelectedObject?.obj as PackageViewModel;
                SelectionChanged?.Invoke();
            };
            
            Add(treeView);
        }

        public void LoadPackages()
        {
            // on refresh, clear the view to indicate that new things are being loaded.
            treeView.SetTreeViewSource(new List<PackageViewModel>());
            // Get packages from repo
            packages = GetPackages();
            packages = packages.OrderByDescending(p => installedPackages.Any(i => i.Name == p.Name)).ThenBy(p => p.Group + p.Name).ToList();
            treeView.SetTreeViewSource(packages);
        }

        List<PackageViewModel> GetPackages()
        {
            var list = new List<PackageViewModel>();
            
            foreach (var repository in PackageManagerSettings.Current.Repositories)
            {
                if (repository.IsEnabled == false)
                    continue;
                if (repository.Manager is HttpPackageRepository httpRepository)
                    list.AddRange(GetHttpPackages(httpRepository));
                else if (repository.Manager is FilePackageRepository fileRepository)
                    list.AddRange(GetFilePackages(fileRepository));
            }

            return list;
        }

        List<PackageViewModel> GetFilePackages(FilePackageRepository repository)
        {
            TuiPm.Log.Info("Loading packages from: " + repository.Url);
            return repository.GetPackages(new PackageSpecifier(null, null), TuiPm.CancellationToken).Select(p => new PackageViewModel(p)).ToList();
        }

        List<PackageViewModel> GetHttpPackages(HttpPackageRepository repository)
        {
            TuiPm.Log.Info("Loading packages from: " + repository.Url);
            var list = new List<PackageViewModel>();

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
            var response = hc.PostAsync(repository.Url.TrimEnd('/') + "/3.1/Query", content, TuiPm.CancellationToken).Result;
            var jsonData = response.Content.ReadAsStringAsync().Result;
    
            // Remove unicode chars
            jsonData = Regex.Replace(jsonData, @"[^\u0000-\u007F]+", string.Empty);
    
            // Parse the json response data
            var jsonPackages = (JsonElement)JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData)["packages"];
            foreach (var item in jsonPackages.EnumerateArray())
            {
                var package = JsonSerializer.Deserialize<PackageViewModel>(item.GetRawText()); 
                if (list.Contains(package) == false)
                    list.Add(package);
            }
            
            // Get installed packages
            foreach (var installedPackage in installedPackages)
            {
                var existing = list.FirstOrDefault(p => p.Name == installedPackage.Name);
                if (existing == null)
                {
                    list.Add(new PackageViewModel()
                    {
                        Name = installedPackage.Name,
                        Description = installedPackage.Description,
                        Group = installedPackage.Group,
                        Owner = installedPackage.Owner
                    });
                }
            }
        
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

        public PackageViewModel()
        {
            
        }

        public PackageViewModel(PackageDef package)
        {
            Name = package.Name;
            Version = package.Version;
            OS = package.OS;
            Architecture = package.Architecture;
            Group = package.Group;
            Owner = package.Owner;
            SourceUrl = package.SourceUrl;
            Description = package.Description;
            Dependencies = package.Dependencies;
        }

        /// <summary>Returns the hash code for this PackageIdentifier.</summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int num = 0;
            if (this.Name != null)
                num ^= this.Name.GetHashCode();
            if (this.Version != (SemanticVersion) null)
                num ^= this.Version.GetHashCode();
            if (this.OS != null)
                num ^= this.OS.GetHashCode();
            return num ^ this.Architecture.GetHashCode();
        }

        /// <summary>Compare this PackageIdentifier to another object.</summary>
        public override bool Equals(object obj)
        {
            return obj is IPackageIdentifier packageIdentifier && packageIdentifier.Name == this.Name && (packageIdentifier.Version == this.Version && packageIdentifier.OS == this.OS) && packageIdentifier.Architecture == this.Architecture;
        }
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
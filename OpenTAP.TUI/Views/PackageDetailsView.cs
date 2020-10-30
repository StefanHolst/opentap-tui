using System;
using System.Linq;
using System.Text;
using System.Xml;
using OpenTap.Package;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class PackageDetailsView : View
    {
        private TextView nameView { get; set; }
        
        public PackageDetailsView()
        {
            nameView = new TextView()
            {
                ReadOnly = true
                // CanFocus = false,
                // Height = Dim.Percent(50)
            };
            
            Add(nameView);
        }

        string parseDescription(string description)
        {
            var xml = new XmlDocument();
            xml.LoadXml($"<Description>{description}</Description>");

            var sb = new StringBuilder();

            void parseXmlNode(object node)
            {
                if (node is XmlElement element)
                {
                    if (element.Name.Equals("Link", StringComparison.OrdinalIgnoreCase))
                    {
                        var attributes = element.Attributes.Cast<XmlAttribute>();
                        var name = attributes.FirstOrDefault(a => a.Name.Equals("name", StringComparison.OrdinalIgnoreCase)).InnerText;
                        var url = attributes.FirstOrDefault(a => a.Name.Equals("url", StringComparison.OrdinalIgnoreCase)).InnerText;
                        sb.AppendLine($"{name.Trim()}: {url.Trim()}");
                    }
                    else if (element.Name.Equals("Contact", StringComparison.OrdinalIgnoreCase))
                    {
                        var attributes = element.Attributes.Cast<XmlAttribute>();
                        var name = attributes.FirstOrDefault(a => a.Name.Equals("name", StringComparison.OrdinalIgnoreCase)).InnerText;
                        var email = attributes.FirstOrDefault(a => a.Name.Equals("email", StringComparison.OrdinalIgnoreCase)).InnerText;
                        sb.AppendLine($"{name.Trim()}: {email.Trim()}");
                    }
                    else
                    {
                        sb.AppendLine();
                        sb.AppendLine(element.Name + ":");
                        foreach (var childNode in element.ChildNodes)
                            parseXmlNode(childNode);
                    }
                }
                else if (node is XmlText text)
                {
                    var values = text.InnerText.Split('\n').Select(x => x.Trim()).Where(x => x != "");
                    sb.AppendLine(string.Join("\n", values));
                }
            }
            
            foreach (var item in xml["Description"])
            {
                parseXmlNode(item);
            }

            return sb.ToString().Replace("\r", "");
        }

        public void LoadPackage(PackageViewModel package, Installation installation, PackageDef installedOpentap)
        {
            if (package == null) // a group
            {
                nameView.Text = "";
                return;
            }

            var isCompatible = package.IsPlatformCompatible();
            if (package.Dependencies != null)
            {
                var opentapDependency = package.Dependencies.FirstOrDefault(d => d.Name.Equals("opentap", StringComparison.OrdinalIgnoreCase));
                if (opentapDependency != null)
                {
                    if (opentapDependency.Version.IsCompatible(installedOpentap.Version) == false)
                        isCompatible = false;
                }
            }

            nameView.Text = $"Name: {package.Name}{(package.isInstalled ? " (installed)" : "")}\n" +
                            (package.Owner != null ? $"Owner: {package.Owner}\n" : "") + 
                            (package.SourceUrl != null ? $"SourceUrl: {package.SourceUrl}\n" : "") + 
                            $"{(Application.Current is PackageVersionSelectorWindow ? "" : "Latest ")}Version: {package.Version}\n" +
                            (package.Architecture != CpuArchitecture.Unspecified ? $"Architecture: {package.Architecture}\n" : "") + 
                            (package.OS != null ? $"OS: {package.OS}\n" : "") + 
                            (package.Dependencies != null ? $"Dependencies: {string.Join(", ", package.Dependencies.Select(p => $"{p.Name}:{p.Version}"))}\n" : "") + 
                            (package.OS != null || package.Architecture != CpuArchitecture.Unspecified ? $"Compatible: {(isCompatible ? "Yes" : "No")}\n\n" : "") + 
                            $"Description: \n{parseDescription(package.Description)}";
        }
    }
}
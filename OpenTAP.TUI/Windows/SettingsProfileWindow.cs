using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class SettingsProfileWindow : BaseWindow
    {
        private ListView listView;
        private Button deleteButton;
        private string Group;
        private string SettingsDir;
        private List<string> Profiles;
        private string CurrentProfile;

        public SettingsProfileWindow(string group) : base(group)
        {
            Group = group;
            Border.Child.RefreshColorScheme();
            
            var listFrame = new FrameView("Profiles")
            {
                Height = Dim.Fill(),
                Width = Dim.Fill(30)
            };
            Add(listFrame);
            var buttonFrame = new FrameView()
            {
                X = Pos.Right(listFrame),
                Height = Dim.Fill(),
                Width = Dim.Fill()
            };
            Add(buttonFrame);
            
            
            // Add profiles to list
            listView = new ListView()
            {
                AllowsMarking = true,
                AllowsMultipleSelection = false,
                Width = Dim.Fill(),
                Height = Dim.Fill(2)
            };
            listView.MarkUnmarkChanged += MarkUnmarkChanged;
            listFrame.Add(listView);

            var newButton = new Button(0, 0, "New Profile");
            newButton.Clicked += () => 
            {
                var request = new NewProfileRequest();
                UserInput.Request(request);

                if (request.Submit == NewProfileRequest.NewProfileSubmit.Add && request.Name != null && (request.Name.Contains("/") || request.Name.Contains("\\") == false))
                {
                    // Add new profile
                    var profilePath = Path.Combine(SettingsDir, request.Name);
                    Directory.CreateDirectory(Path.Combine(SettingsDir, profilePath));
                    ComponentSettings.SetSettingsProfile(Group, Path.Combine(SettingsDir, profilePath));
                    LoadProfiles();
                }
            };
            
            buttonFrame.Add(newButton);
            deleteButton = new Button(0, 1, "Delete Profile");
            deleteButton.Clicked += () =>
            {
                ComponentSettings.SetSettingsProfile(Group, Path.Combine(SettingsDir, Profiles.FirstOrDefault(p => p != CurrentProfile) ?? "Default"));

                try
                {
                    Directory.Delete(Path.Combine(SettingsDir, CurrentProfile), true);
                }
                catch
                {
                    // ignored
                }

                LoadProfiles();
            };
            buttonFrame.Add(deleteButton);
            
            var exportButton = new Button(0, 2, "Export Profile");
            exportButton.Clicked += () =>
            {
                // Exporting is just zipping the settings files
                var dialog = new SaveDialog("Export Settings", "Export");
                Application.Run(dialog);
                if (dialog.FileName != null)
                {
                    try
                    {
                        var exportPath = Path.Combine(dialog.DirectoryPath.ToString(), dialog.FilePath.ToString());
                        var settingsPath = Path.Combine(SettingsDir, CurrentProfile);
                        ZipFile.CreateFromDirectory(settingsPath, exportPath);
                    }
                    catch (Exception e)
                    {
                        TUI.Log.Error(e.Message);
                        TUI.Log.Debug(e);
                    }
                }
            };
            buttonFrame.Add(exportButton);
            
            var importButton = new Button(0, 3, "Import Profile");
            importButton.Clicked += () =>
            {
                // Importing is just unzipping the settings files
                var dialog = new OpenDialog("Import Settings", "Import");
                Application.Run(dialog);
                if (dialog.FilePath != null)
                {
                    try
                    {
                        var importPath = Path.Combine(dialog.DirectoryPath.ToString(), dialog.FilePath.ToString());
                        var settingsPath = Path.Combine(SettingsDir, Path.GetFileNameWithoutExtension(importPath));
                        ZipFile.ExtractToDirectory(importPath, settingsPath);
                        LoadProfiles();
                    }
                    catch (Exception e)
                    {
                        TUI.Log.Error(e.Message);
                        TUI.Log.Debug(e);
                    }
                }
            };
            buttonFrame.Add(importButton);
            
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            SettingsDir = ComponentSettings.GetSettingsDirectory(Group, false);
            
            // Get directories in Settings dir
            Profiles = Directory.GetDirectories(SettingsDir).Select(d => Path.GetFileName(d)).ToList();
            if (Profiles.Any() == false)
                Profiles.Add("Default");
            
            // Get current profile
            CurrentProfile = ComponentSettings.GetSettingsDirectory(Group).Substring(Path.Combine(ComponentSettings.SettingsDirectoryRoot, Group).Length + 1);
            
            // Update listview
            listView.SetSource(Profiles);
            listView.Source.SetMark(Profiles.IndexOf(CurrentProfile), true);

            deleteButton.Enabled = Profiles.Count > 1;
        }

        private void MarkUnmarkChanged(bool isMarked, int selectedItem)
        {
            if (isMarked == false)
            {
                // Should we allow this?
                listView.Source.SetMark(selectedItem, true);
            }
            else
            {
                // Change profile
                CurrentProfile = Profiles[selectedItem];
                ComponentSettings.SetSettingsProfile(Group, Path.Combine(SettingsDir, CurrentProfile));
            }
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (KeyMapHelper.IsKey(keyEvent, KeyTypes.Cancel))
            {
                Application.RequestStop();
                return true;
            }
            return base.ProcessKey(keyEvent);
        }
    }

    [Display("New Profile")]
    public class NewProfileRequest : ValidatingObject
    {
        public enum NewProfileSubmit
        {
            Add = 1,
            Cancel = 2,
        }

        public string Name { get; set; }
        
        [Submit]
        [Layout(LayoutMode.FullRow | LayoutMode.FloatBottom, 1, 1000)]
        public NewProfileSubmit Submit { get; set; } = NewProfileSubmit.Cancel;

        public NewProfileRequest()
        {
            Rules.Add(() => Name?.Contains("/") != true || Name?.Contains("\\") != true, "Profile name cannot contain '/'.", nameof(Name));
        }
    }
}
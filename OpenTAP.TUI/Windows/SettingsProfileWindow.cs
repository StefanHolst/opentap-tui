using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui.Windows
{
    public class SettingsProfileWindow : Window
    {
        private ListView listView;
        private HelperButtons helperButtons;

        public SettingsProfileWindow(string group)
        {
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
            
            
            // get directories in Settings dir
            var settingsDir = ComponentSettings.GetSettingsDirectory(group, false);
            var profiles = Directory.GetDirectories(settingsDir).Select(d => Path.GetFileName(d)).ToList();
            if (profiles.Any() == false)
                profiles.Add("Default");
            // Add profiles to list
            listView = new ListView(profiles)
            {
                AllowsMarking = true,
                AllowsMultipleSelection = false,
                Width = Dim.Fill(),
                Height = Dim.Fill(2)
            };
            listView.MarkUnmarkChanged += MarkUnmarkChanged;
            listFrame.Add(listView);
            // Set current profile
            var currentProfile = ComponentSettings.GetSettingsDirectory(group).Substring(Path.Combine(ComponentSettings.SettingsDirectoryRoot, group).Length + 1);
            listView.Source.SetMark(profiles.IndexOf(currentProfile), true);

            /// TODO:
            /// - If deleted profile was the selected one, select the default one
            /// - Don't allow deleting the last profile
            /// - Add try catch around delete folder
            /// - Select new profle as selected profile
            /// - Add support for changing profile
            /// - Don't allow no selected profile
            /// - Add import/export
            

            var newButton = new Button(0, 0, "New Profile");
            newButton.Clicked += () => 
            {
                var request = new NewProfileRequest();
                UserInput.Request(request);

                if (request.Submit == NewProfileRequest.NewProfileSubmit.Add && request.Name != null && (request.Name.Contains("/") || request.Name.Contains("\\") == false))
                {
                    // Add new profile
                    Directory.CreateDirectory(Path.Combine(settingsDir, request.Name));
                }
            };
            buttonFrame.Add(newButton);
            var deleteButton = new Button(0, 1, "Delete Profile");
            deleteButton.Clicked += () =>
            {
                var profile = profiles[listView.SelectedItem];
                Directory.Delete(Path.Combine(settingsDir, profile));
                
                // If deleted profile was the selected one, select the default one
            };
            buttonFrame.Add(deleteButton);
            // var exportButton = new Button(0, 2, "Export Profile");
            // exportButton.Clicked += () =>
            // {
            //     
            // };
            // buttonFrame.Add(exportButton);
            // var importButton = new Button(0, 3, "Import Profile");
            // importButton.Clicked += () =>
            // {
            //     
            // };
            // buttonFrame.Add(importButton);
        }

        private void MarkUnmarkChanged(bool isMarked, int selectedItem)
        {
            if (isMarked == false)
            {
                // Should we allow this?
            }
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
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
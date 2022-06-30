using System;
using System.IO;
using OpenTap;
using OpenTap.Tui;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    public class FilePathEditProvider : IPropEditProvider
    {
        public int Order => 0;

        public View Edit(AnnotationCollection annotation)
        {
            var filePath = annotation.Get<FilePathAttribute>();
            if (filePath == null)
                return null;
            
            FileDialog dialog;
            if (filePath.Behavior == FilePathAttribute.BehaviorChoice.Open)
                dialog = new OpenDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Open"};
            else
                dialog = new SaveDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Save" };

            dialog.Removed += view =>
            {
                try
                {
                    var value = annotation.Get<IObjectValueAnnotation>().Value;
                    var dialogUri = new Uri(dialog.FilePath.ToString());
                    var workingUri = new Uri(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
                    var path = workingUri.MakeRelativeUri(dialogUri).ToString();
                    if (value is MacroString ms)
                        ms.Text = path;
                    else
                        annotation.Get<IStringValueAnnotation>().Value = path;
                }
                catch (Exception exception)
                {
                    TUI.Log.Error($"{exception.Message} {DefaultExceptionMessages.DefaultExceptionMessage}");
                    TUI.Log.Debug(exception);
                }
            };
            
            return dialog;
        }
    }
}

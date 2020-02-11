using System;
using System.Collections.Generic;
using System.Text;
using OpenTap;
using OpenTap.TUI;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class FilePathEditProvider : IPropEditProvider
    {
        public int Order => 0;

        public View Edit(AnnotationCollection annotation)
        {
            var filePath = annotation.Get<FilePathAttribute>();
            if (filePath == null)
                return null;

            // Parse extension patterns
            var exts = filePath.FileExtension.Split('|');
            string[] extPatterns = null;
            if (string.IsNullOrEmpty(filePath.FileExtension) || !filePath.FileExtension.Contains("*."))
                extPatterns = new []{ filePath.FileExtension };
            else if (filePath.FileExtension.Contains("*.*") == false)
            {
                extPatterns = new string[exts.Length / 2];
                
                for (int i = 0; i < exts.Length;)
                {
                    extPatterns[i / 2] = exts[i + 1].TrimStart().TrimStart('*');
                    i += 2;
                }
            }
            
            FileDialog dialog;
            if (filePath.Behavior == FilePathAttribute.BehaviorChoice.Open)
                dialog = new OpenDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Open", AllowedFileTypes = extPatterns};
            else
                dialog = new SaveDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Save" };
            
            dialog.SelectionChanged += (sender) =>
            {
                try
                {
                    var path = sender.FilePath;
                    var value = annotation.Get<IObjectValueAnnotation>().Value;
                    if (value is MacroString ms)
                        ms.Text = path.ToString();
                    else
                        annotation.Get<IStringValueAnnotation>().Value = path.ToString();
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

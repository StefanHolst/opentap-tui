using System;
using System.Collections.Generic;
using System.Text;
using OpenTap;
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

            FileDialog dialog;
            if (filePath.Behavior == FilePathAttribute.BehaviorChoice.Open)
                dialog = new OpenDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Open" };
            else
                dialog = new SaveDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Save" };
            
            dialog.SelectionChanged += (sender) =>
            {
                var path = sender.FilePath;
                if (string.IsNullOrWhiteSpace(filePath.FileExtension) == false && path.ToLower().EndsWith(filePath.FileExtension) == false)
                {
                    TUI.Log.Info($"Extension of '{path}' does not match '.{filePath.FileExtension}'.");
                    return;
                }
                
                var value = annotation.Get<IObjectValueAnnotation>().Value;
                if (value is MacroString ms)
                    ms.Text = path.ToString();
                else
                    annotation.Get<IStringValueAnnotation>().Value = path.ToString();
            };
            
            return dialog;
        }
    }
}

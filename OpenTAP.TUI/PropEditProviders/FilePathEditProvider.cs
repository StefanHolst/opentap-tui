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
            
            FileDialog dialog;
            if (filePath.Behavior == FilePathAttribute.BehaviorChoice.Open)
                dialog = new OpenDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Open"};
            else
                dialog = new SaveDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Save" };

            dialog.SelectionChanged += (path) =>
            {
                try
                {
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

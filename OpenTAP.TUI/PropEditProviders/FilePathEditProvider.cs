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

            if (filePath.Behavior == FilePathAttribute.BehaviorChoice.Open)
            {
                var dialog = new OpenDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Open" };
                dialog.SelectionChanged += (s, path) => 
                {
                    var value = annotation.Get<IObjectValueAnnotation>().Value;

                    if (value is MacroString ms)
                        ms.Text = path.ToString();
                    else
                        value = path.ToString();
                };

                return dialog;
            }
            else
            {
                var dialog = new SaveDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Save" };
                dialog.SelectionChanged += (s, path) =>
                {
                    var value = annotation.Get<IObjectValueAnnotation>().Value;

                    if (value is MacroString ms)
                        ms.Text = path.ToString();
                    else
                        value = path.ToString();
                };

                return dialog;
            }
        }
    }
}

using System;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    public class BooleanEditProvider : IPropEditProvider
    {
        public int Order => 100;
        public View Edit(AnnotationCollection annotation, bool isReadOnly)
        {
            var booledit = annotation.Get<IObjectValueAnnotation>();
            if (booledit == null || annotation.Get<IMemberAnnotation>()?.ReflectionInfo != TypeData.FromType(typeof(bool))) return null;

            var check = new CheckBox(annotation.Get<DisplayAttribute>()?.Name ?? "...", (bool)booledit.Value);
            check.Toggled += b => 
            {
                try
                {
                    // Since readonly is not a thing, we have to disable the checkbox by swapping its value back to what it was.
                    if (isReadOnly)
                    {
                        check.Checked = !check.Checked;
                    }
                    booledit.Value = check.Checked;
                }
                catch (Exception exception)
                {
                    TUI.Log.Error(exception.Message);
                    TUI.Log.Debug(exception);
                }
            };

            return check;
        }
    }
}

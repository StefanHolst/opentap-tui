using OpenTap;
using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class BooleanEditProvider : IPropEditProvider
    {
        public int Order => 100;
        public View Edit(AnnotationCollection annotation)
        {
            var booledit = annotation.Get<IObjectValueAnnotation>();
            if (booledit == null || annotation.Get<IMemberAnnotation>()?.ReflectionInfo != TypeData.FromType(typeof(bool))) return null;

            var check = new CheckBox(annotation.Get<DisplayAttribute>()?.Name ?? "...", (bool)booledit.Value);
            check.Toggled += (sender, args) => booledit.Value = check.Checked;

            return check;
        }
    }
}

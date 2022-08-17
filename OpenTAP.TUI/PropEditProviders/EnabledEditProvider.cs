using System;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    public class EnabledEditProvider : IPropEditProvider
    {
        public int Order => 5;
        public View Edit(AnnotationCollection annotation)
        {
            var members = annotation.Get<IMembersAnnotation>()?.Members.ToArray();
            if (members == null || members.Length != 2)
                return null;
            int enabledIndex = members[0].Get<IMemberAnnotation>().Member.Name == "IsEnabled" ? 0 : 1;

            if (members[enabledIndex].Any(a => a.GetType().Name == "BooleanValueAnnotation") == false)
                return null;
            
            var enabled = members[enabledIndex];
            var value = members[enabledIndex == 0 ? 1 : 0];
            var check = new CheckBox(annotation.Get<DisplayAttribute>().Name, enabled.Get<IObjectValueAnnotation>()?.Value != null && (bool)enabled.Get<IObjectValueAnnotation>().Value);
            
            var viewbox = new View();
            viewbox.Add(check);
            
            var valuebox = PropEditProvider.GetProvider(value, out var _);
            if (valuebox == null) return null;
            valuebox.Y = Pos.Bottom(check) + 1;
            valuebox.Width = Dim.Fill();
            valuebox.Enabled = check.Checked;
            viewbox.Add(valuebox);
            
            check.Toggled += b => 
            {
                try
                {
                    enabled.Get<IObjectValueAnnotation>().Value = check.Checked;
                    valuebox.Enabled = check.Checked;
                }
                catch (Exception exception)
                {
                    TUI.Log.Error($"{exception.Message} {DefaultExceptionMessages.DefaultExceptionMessage}");
                    TUI.Log.Debug(exception);
                }
            };
            
            return viewbox;
        }
    }
}

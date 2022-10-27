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
            var enabledValueAnnotation = annotation.Get<IEnabledValueAnnotation>();
            if (enabledValueAnnotation == null)
                return null;

            var enabled = enabledValueAnnotation.IsEnabled;
            var value = enabledValueAnnotation.Value;
            var check = new CheckBox(annotation.Get<DisplayAttribute>().Name, enabled.Get<IObjectValueAnnotation>()?.Value != null && (bool)enabled.Get<IObjectValueAnnotation>().Value);
            check.Height = 1;
            var viewbox = new View();
            viewbox.Add(check);
            
            var valuebox = PropEditProvider.GetProvider(value, out var _);
            if (valuebox == null) return null;
            valuebox.Y = Pos.Bottom(check);
            valuebox.X = 1;
            valuebox.Width = Dim.Fill();
            valuebox.Height = Dim.Fill();
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

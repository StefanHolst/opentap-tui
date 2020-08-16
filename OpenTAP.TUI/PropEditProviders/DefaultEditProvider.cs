using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using OpenTap;
using OpenTap.Tui;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class DefaultEditProvider : IPropEditProvider
    {
        public int Order => 1000;
        public View Edit(AnnotationCollection annotation)
        {
            var stredit = annotation.Get<IStringValueAnnotation>();
            if (stredit == null) return null;
            var text = stredit.Value ?? "";
            var textField = new TextViewWithEnter(){Text = text};
            textField.ReadOnly = annotation.Get<IAccessAnnotation>()?.IsReadOnly ?? false;
            
            LayoutAttribute layout = annotation.Get<IMemberAnnotation>()?.Member.GetAttribute<LayoutAttribute>();
            if ((layout?.RowHeight ?? 0) > 1)
            {
                // support multiline edit boxes.
                textField.CloseOnEnter = false;
            }
            
            textField.Closing += () => 
            {
                try
                {
                    stredit.Value = textField.Text.ToString().Replace("\r", "");
                }
                catch (Exception exception)
                {
                    TUI.Log.Error($"{exception.Message} {DefaultExceptionMessages.DefaultExceptionMessage}");
                    TUI.Log.Debug(exception);
                }
            };
            return textField;
        }
    }
}

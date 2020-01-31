using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using OpenTap;
using OpenTap.TUI;
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
            LayoutAttribute layout = annotation.Get<IMemberAnnotation>()?.Member.GetAttribute<LayoutAttribute>();
            if ((layout?.RowHeight ?? 0) > 1)
            {
                // support multiline edit boxes.
                textField.CloseOnEnter = false;
            }
            
            textField.Closing += (s, e) => 
            {
                try
                {
                    if (e) // trim \r from the output.
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

    class TextViewWithEnter : TextView
    {
        public bool CloseOnEnter { get; set; } = true;
        public override bool ProcessKey(KeyEvent kb)
        {
            if (kb.Key == Key.Enter && CloseOnEnter )
                return false;

            if (kb.Key == Key.Esc && !CloseOnEnter)
            {
                // invoke a new Enter command while accepting to closing on it.
                CloseOnEnter = true;
                Application.Current.ProcessKey(new KeyEvent() {Key = Key.Enter});
                return true;
            }
            return base.ProcessKey(kb);
        }
    }
}

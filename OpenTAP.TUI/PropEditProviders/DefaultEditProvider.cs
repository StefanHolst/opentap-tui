using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class DefaultEditProvider : IPropEditProvider
    {
        public int Order => 1000;
        private IStringValueAnnotation Annotation;
        public View Edit(AnnotationCollection annotation)
        {
            var stredit = annotation.Get<IStringValueAnnotation>();
            if (stredit == null) return null;
            var textField = new TextField(stredit.Value);
            textField.Changed += (sender, args) => stredit.Value = textField.Text.ToString();
            return textField;
        }

        public void Commit(View view)
        {
            
        }
    }
}

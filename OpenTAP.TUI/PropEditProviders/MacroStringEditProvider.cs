using System;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    /// <summary> Control for editing secure strings. </summary>
    public class MacroStringEditProvider : IPropEditProvider
    {
        public int Order => 0;
        public View Edit(AnnotationCollection annotation, bool isReadOnly)
        {
            var isString = annotation.Get<IReflectionAnnotation>().ReflectionInfo.DescendsTo(typeof(MacroString));
            if (isString == false)
                return null;

            if (!(annotation.Get<IObjectValueAnnotation>().Value is MacroString ms))
                return null;

            var textField = new Views.TextViewWithEnter()
            {
                Text = ms.Text,
                Height = Dim.Fill(1),
                CloseOnEnter = true
            };

            textField.Closing += () =>
            {
                try
                {
                    ms.Text = (string)textField.Text;
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
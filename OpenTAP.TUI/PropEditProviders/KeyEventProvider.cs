using System;
using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    public class KeyEventProvider : IPropEditProvider
    {
        public int Order { get; }
        public View Edit(AnnotationCollection annotation, bool isReadOnly)
        {
            var keyMapSetting = annotation.Get<IObjectValueAnnotation>();
            if (keyMapSetting.Value is KeyEvent keyEvent)
            {
                var keyMapBindingView = new KeyMapBindingView(keyEvent);
                keyMapBindingView.Closing += changed => 
                {
                    try
                    {
                        if (changed && keyMapBindingView.NewKeyMap != null)
                            keyMapSetting.Value = keyMapBindingView.NewKeyMap;
                    }
                    catch (Exception exception)
                    {
                        TUI.Log.Error($"{exception.Message} {DefaultExceptionMessages.DefaultExceptionMessage}");
                        TUI.Log.Debug(exception);
                    }
                };

                return keyMapBindingView;
            }

            return null;
        }
    }
}
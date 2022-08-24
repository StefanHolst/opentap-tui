using System;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    public class EnumEditProvider : IPropEditProvider
    {
        public int Order => 10;
        public View Edit(AnnotationCollection annotation)
        {
            var availableValue = annotation.Get<IAvailableValuesAnnotationProxy>();
            if (availableValue == null)
                return null;

            var availableValues = availableValue.AvailableValues.ToArray();
            var listView = new ListView(availableValues.Select(p => 
                p.Get<IStringReadOnlyValueAnnotation>()?.Value ?? 
                p.Get<IObjectValueAnnotation>().Value).ToList());

            listView.KeyPress += args =>
            {
                if (KeyMapHelper.IsKey(args.KeyEvent, KeyTypes.Select))
                {
                    try
                    {
                        if (availableValues.Any())
                            availableValue.SelectedValue = availableValues[listView.SelectedItem];
                    }
                    catch (Exception exception)
                    {
                        TUI.Log.Error($"{exception.Message} {DefaultExceptionMessages.DefaultExceptionMessage}");
                        TUI.Log.Debug(exception);
                    }
                }
            };

            var index = Array.IndexOf(availableValues, availableValue.SelectedValue);
            if (index != -1)
            {
                listView.SelectedItem = index;
                listView.TopItem = Math.Max(0, index - Application.Current.Bounds.Height);
            }

            return listView;
        }
    }
}

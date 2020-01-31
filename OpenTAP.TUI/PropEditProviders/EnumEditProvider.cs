using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
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
            listView.Closing += (s, e) =>
            {
                if (availableValues.Any())
                    availableValue.SelectedValue = availableValues[listView.SelectedItem];
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

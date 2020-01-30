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

    public class MultiSelectProvider : IPropEditProvider
    {
        class ViewWrapper : View
        {
            public readonly ListView InnerView;
            public readonly AnnotationCollection _annotation;

            public ViewWrapper(ListView innerView, AnnotationCollection annotation)
            {
                InnerView = innerView;
                _annotation = annotation;
                Add(innerView);
            }
        }
        public int Order { get; } = 9;
        public View Edit(AnnotationCollection annotation)
        {
            var multi = annotation.Get<IMultiSelectAnnotationProxy>();
            if (multi == null) return null;
            var avail = annotation.Get<IAvailableValuesAnnotationProxy>();
            if (avail == null) return null;
            var avalues = avail.AvailableValues.ToArray();
  
            var view = new ListView(avalues.Select(x => x.Get<IStringReadOnlyValueAnnotation>()?.Value ?? "?").ToArray());
            view.AllowsMarking = true;

            for (int i = 0; i < avalues.Length; i++)
            {
                if (multi.SelectedValues.Contains(avalues[i]))
                    view.Source.SetMark(i, true);
            }

            view.Closing += (s, e) => 
            {
                var selectedValues = new List<AnnotationCollection>();
                for (int i = 0; i < view.Source.Count; i++)
                {
                    if (view.Source.IsMarked(i))
                        selectedValues.Add(avalues[i]);
                }
                multi.SelectedValues = selectedValues;
            };
            
            return new ViewWrapper(view, annotation);
        }
    }
}

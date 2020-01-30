using System.Collections.Generic;
using System.Linq;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class MultiSelectProvider : IPropEditProvider
    {
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
            
            return view;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
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
  
            var view = new ListView(avalues.Select(x => x.Get<IStringReadOnlyValueAnnotation>()?.Value ?? x.Get<IObjectValueAnnotation>()?.Value ?? "?").ToArray());
            view.AllowsMarking = true;

            for (int i = 0; i < avalues.Length; i++)
            {
                if (multi.SelectedValues.Contains(avalues[i]))
                    view.Source.SetMark(i, true);
            }

            view.Removed += x => 
            {
                try
                {
                    var selectedValues = new List<AnnotationCollection>();
                    for (int i = 0; i < view.Source.Count; i++)
                    {
                        if (view.Source.IsMarked(i))
                            selectedValues.Add(avalues[i]);
                    }
                    multi.SelectedValues = selectedValues;
                }
                catch (Exception exception)
                {
                    TUI.Log.Error($"{exception.Message} {DefaultExceptionMessages.DefaultExceptionMessage}");
                    TUI.Log.Debug(exception);
                }
            };
            
            return view;
        }
    }
}
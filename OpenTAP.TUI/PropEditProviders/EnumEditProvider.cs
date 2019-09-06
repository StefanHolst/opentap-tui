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
            var avail = annotation.Get<IAvailableValuesAnnotationProxy>();
            if (avail == null) return null;
            var avalues = avail.AvailableValues.ToArray();
            var listView = new ListView(avalues.Select(p => p.Get<IStringReadOnlyValueAnnotation>()?.Value).ToList());
            listView.SelectedItem = Array.IndexOf(avalues, avail.SelectedValue);
            if(listView.SelectedItem != -1)
                listView.TopItem = 0;
            listView.SelectedChanged += () => avail.SelectedValue = avalues[listView.SelectedItem];
            return listView;
        }

        public void Commit(View view)
        {
            
        }


        public void Edit(PropertyInfo prop, object obj)
        {
            
            var availableValues = Enum.GetValues(prop.PropertyType);

            var win = new EditWindow(prop.Name);
            var listView = new ListView(availableValues);
            win.Add(listView);

            Application.Run(win);

            if (win.Edited && availableValues.Length > 0)
                prop.SetValue(obj, availableValues.GetValue(listView.SelectedItem));
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
            
            return new ViewWrapper(view, annotation);
        }

        public void Commit(View view)
        {
            
        }
    }

    [Flags]
    public enum FlagsTest
    {
        A = 1,
        B = 2,
        C = 4
    }
    public class PropertiesTest : TestStep
    {
        public FlagsTest FlagEnum { get; set; }
        public Enabled<string> X { get; set; } = new Enabled<string>(){Value =  "5", IsEnabled = true};
        public override void Run()
        {
            
        }
    }
}

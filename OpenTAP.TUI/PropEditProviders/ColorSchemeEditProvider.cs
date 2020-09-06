using System.ComponentModel;
using OpenTap;
using OpenTap.Tui;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class ColorSchemeEditProvider : IPropEditProvider
    {
        public int Order { get; } = 0;
        public View Edit(AnnotationCollection annotation)
        {
            var coloredit = annotation.Get<IObjectValueAnnotation>();
            if (coloredit == null || annotation.Get<IMemberAnnotation>()?.ReflectionInfo != TypeData.FromType(typeof(ColorSchemeViewmodel))) return null;

            var vm = coloredit.Value as ColorSchemeViewmodel;
            var propertiesView = new PropertiesView();
            propertiesView.LoadProperties(vm);
            
            propertiesView.Removed += view =>
            {
                // coloredit.Value = vm;
                // var test = (ColorSchemeViewmodel) coloredit.Value;
                // test.FocusBackground = vm.FocusBackground;
                // annotation.Write();
                // annotation.Read();
            };
            
            return propertiesView;
        }
    }
    
}
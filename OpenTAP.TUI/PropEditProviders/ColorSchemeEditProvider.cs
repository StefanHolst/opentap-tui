using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
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
            
            return propertiesView;
        }
    }
}
using Terminal.Gui;

namespace OpenTap.Tui.Annotations
{
    public class ColorSchemeAnnotation : IStringReadOnlyValueAnnotation
    {
        AnnotationCollection annotation;
    
        public ColorSchemeAnnotation(AnnotationCollection a) => annotation = a;
        
        public string Value
        {
            get
            {
                var colorScheme = annotation.Get<IObjectValueAnnotation>()?.Value as ColorScheme;
                if (colorScheme == null) return base.ToString();
                
                
                return $"{colorScheme.Normal.Foreground} | {colorScheme.Normal.Background} | {colorScheme.Focus.Foreground} | {colorScheme.Focus.Background} | {colorScheme.HotNormal.Foreground} | {colorScheme.HotNormal.Background} | {colorScheme.HotFocus.Foreground} | {colorScheme.HotFocus.Background}";
            }
        }
    }
    
    public class ColorSchemeAnnotator : IAnnotator
    {
        public void Annotate(AnnotationCollection annotations)
        {
            var colorScheme = annotations.Get<IObjectValueAnnotation>()?.Value as ColorScheme;
            if (colorScheme == null) return;
            
            annotations.Add(new ColorSchemeAnnotation(annotations));
        }
    
        public double Priority => 1;
    }
    
    public class ColorAttributeAnnotation : IStringReadOnlyValueAnnotation
    {
        AnnotationCollection annotation;
    
        public ColorAttributeAnnotation(AnnotationCollection a) => annotation = a;
        
        public string Value
        {
            get
            {
                var obj = annotation.Get<IObjectValueAnnotation>()?.Value;
                if (obj == null || obj is Attribute == false) return base.ToString();

                var colorAttribute = (Attribute)obj;
                return $"{colorAttribute.Foreground} | {colorAttribute.Background}";
            }
        }
    }
    public class ColorAttributeAnnotator : IAnnotator
    {
        public void Annotate(AnnotationCollection annotations)
        {
            var colorAttribute = annotations.Get<IObjectValueAnnotation>()?.Value;
            if (colorAttribute == null || colorAttribute is Attribute == false) return;
            
            annotations.Add(new ColorAttributeAnnotation(annotations));
        }
    
        public double Priority => 1;
    }
}
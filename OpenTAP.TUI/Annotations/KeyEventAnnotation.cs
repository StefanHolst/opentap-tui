using Terminal.Gui;

namespace OpenTap.Tui.Annotations
{
    public class KeyEventAnnotation : IStringReadOnlyValueAnnotation
    {
        AnnotationCollection annotation;
    
        public KeyEventAnnotation(AnnotationCollection a) => annotation = a;
        
        public string Value
        {
            get
            {
                var keyEvent = annotation.Get<IObjectValueAnnotation>()?.Value as KeyEvent;
                if (keyEvent == null) return "";

                return keyEvent.ToString();
            }
        }
    }
    
    public class KeyEventAnnotator : IAnnotator
    {
        public void Annotate(AnnotationCollection annotations)
        {
            var keyEvent = annotations.Get<IObjectValueAnnotation>()?.Value as KeyEvent;
            if (keyEvent == null) return;
            
            annotations.Add(new KeyEventAnnotation(annotations));
        }
    
        public double Priority => 1;
    }
}
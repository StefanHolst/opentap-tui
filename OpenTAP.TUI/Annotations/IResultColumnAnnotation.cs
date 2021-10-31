namespace OpenTap.Tui.Annotations
{
    public class IResultColumnAnnotation : IStringReadOnlyValueAnnotation
    {
        AnnotationCollection annotation;

        public IResultColumnAnnotation(AnnotationCollection a) => annotation = a;
        
        public string Value
        {
            get
            {
                var column = annotation.Get<IObjectValueAnnotation>()?.Value as IResultColumn;
                if (column == null) return "";
                
                var title = column.Name;
                if (column.Data.Length > 0)
                    title += $" {column.Data.Length}";
                if (CanConvertToDouble(column) == false)
                    title += " [Unsupported Type]";
                
                return title;
            }
        }
        
        public static bool CanConvertToDouble(IResultColumn column)
        {
            var type = column.Data.GetType();
            var elementType = type.GetElementType();
            return type.IsArray && elementType != typeof(string) && elementType?.GetInterface("IConvertible") != null;
        }
    }
    
    public class IResultColumnAnnotator : IAnnotator
    {
        public void Annotate(AnnotationCollection annotations)
        {
            var column = annotations.Get<IObjectValueAnnotation>()?.Value as IResultColumn;
            if (column == null) return;
            
            annotations.Add(new IResultColumnAnnotation(annotations));
        }

        public double Priority => 1;
    }
}

using System;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    public static class PropEditProvider
    {
        public static View GetProvider(AnnotationCollection annotation, bool isReadOnly, out IPropEditProvider provider)
        {
            provider = null;
            var editProviders = TypeData.FromType(typeof(IPropEditProvider)).DerivedTypes
                .Where(p => p.CanCreateInstance)
                .Select(p => p.CreateInstance(Array.Empty<object>()))
                .Cast<IPropEditProvider>().OrderBy(p => p.Order).ToArray();

            isReadOnly = isReadOnly || !(annotation.Get<IEnabledAnnotation>()?.IsEnabled ?? true) || (annotation.Get<IAccessAnnotation>()?.IsReadOnly ?? false);

            foreach (var item in editProviders)
            {
                try
                {
                    View view = item.Edit(annotation, isReadOnly);
                    if (view != null)
                    {
                        provider = item;
                        return view;
                    }
                }
                catch (Exception ex)
                {
                    TUI.Log.Error(ex);
                }
            }

            return null;
        }
    }
}
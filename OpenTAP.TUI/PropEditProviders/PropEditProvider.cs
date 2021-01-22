using System;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    public static class PropEditProvider
    {
        public static View GetProvider(AnnotationCollection annotation, out IPropEditProvider provider)
        {
            provider = null;
            var editProviders = TypeData.FromType(typeof(IPropEditProvider)).DerivedTypes
                .Where(p => p.CanCreateInstance)
                .Select(p => p.CreateInstance(Array.Empty<object>()))
                .Cast<IPropEditProvider>().OrderBy(p => p.Order).ToArray();

            foreach (var item in editProviders)
            {
                View view = item.Edit(annotation);
                if (view != null)
                {
                    provider = item;
                    return view;
                }
            }

            return null;
        }
    }
}
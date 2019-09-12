using OpenTap;
using OpenTAP.TUI.PropEditProviders;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Terminal.Gui;


namespace OpenTAP.TUI
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

    public class EditWindow : Window
    {
        public bool Edited { get; set; }

        public EditWindow(string title) : base(title)
        {

        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            var handled = base.ProcessKey(keyEvent);

            if (keyEvent.Key == Key.Esc)
            {
                Edited = false;
                Application.RequestStop();
                return true;
            }
            if (keyEvent.Key == Key.Enter && handled != true)
            {
                Edited = true;
                Application.RequestStop();
                return true;
            }

            return handled;
        }
    }
}
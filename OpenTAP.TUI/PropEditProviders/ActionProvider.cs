using System;
using System.Linq;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class ActionProvider : IPropEditProvider
    {
        public int Order => 100;
        public View Edit(AnnotationCollection annotation)
        {
            var actionEdit = annotation.Get<IObjectValueAnnotation>();
            if (actionEdit.Value is Action ac)
            {
                var actionName = annotation.Get<DisplayAttribute>().Name;
                try
                {
                    ac.Invoke();
                }
                catch (Exception ex)
                {
                    TUI.Log.Error(ex);
                }
                
                var message = new MessageDialog($"'{actionName}' was executed.", actionName);
                return message;
            }

            return null;
        }
    }
    
    class MessageDialog : Dialog
    {
        public MessageDialog(string title, string message) : base(null, 50, 7)
        {
            var text = new Label (title) {
                X = Pos.Center(),
                Y = 0,
            };
            Add(text);
            
            var button = new Button ("Ok");
            button.Clicked += () => {
                Application.RequestStop ();
            };
            AddButton (button);
        }
    }

    public class TuiUserInput : IUserInputInterface
    {
        public void RequestUserInput(object dataObject, TimeSpan Timeout, bool modal)
        {
            // var message = new MessageDialog($"'{actionName}' was executed.", actionName);

            var typedata = TypeData.FromType(dataObject.GetType());
            var title = typedata.Display?.Name ?? typedata.Name;

            // var annotations = AnnotationCollection.Annotate(dataObject);
            // var members = annotations.Get<IMembersAnnotation>().Members.ToList();
            // members.AddRange(annotations.Get<IForwardedAnnotations>().Forwarded);

            var message = new MessageDialog(title, "");
            Application.Run(message);
        }
    }
}
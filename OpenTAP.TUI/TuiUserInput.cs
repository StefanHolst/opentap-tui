using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class TuiUserInput : IUserInputInterface
    {
        string getTitle(List<AnnotationCollection> members, object dataObject)
        {
            var title = members.FirstOrDefault(m => m.Get<DisplayAttribute>()?.Name?.Equals("title", StringComparison.OrdinalIgnoreCase) == true)?.Get<IStringValueAnnotation>()?.Value;
            if (title == null)
                title = members.FirstOrDefault(m => m.Get<DisplayAttribute>()?.Name?.Equals("name", StringComparison.OrdinalIgnoreCase) == true)?.Get<IStringValueAnnotation>()?.Value;
            if (title == null)
            {
                var typeData = TypeData.FromType(dataObject.GetType());
                title = typeData.Display?.Name ?? typeData.Name;
            }

            return title;
        }

        public void RequestUserInput(object dataObject, TimeSpan Timeout, bool modal)
        {
            // Load properties
            var annotations = AnnotationCollection.Annotate(dataObject);
            var members = annotations?.Get<IMembersAnnotation>()?.Members?.ToList();
            var propertiesView = new PropertiesView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            propertiesView.LoadProperties(dataObject);
            propertiesView.Submit += () => { Application.Current.Running = false; };
            
            // Create dialog
            var dialogTitle = getTitle(members, dataObject);
            var dialog = new EditWindow(dialogTitle)
            {
                Width = Dim.Percent(50),
                Height = Dim.Percent(50),
                X = Pos.Center(),
                Y = Pos.Center(),
                ColorScheme = Colors.Dialog
            };
            dialog.Add(propertiesView);

            // Show dialog
            var queryRunning = true;
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
            Application.MainLoop.Invoke(() =>
            {
                Application.Run(dialog);
                resetEvent.Set();
                queryRunning = false;
            });

            // Wait for timeout, then close the dialog
            var timedOut = false;
            if (TimeSpan.MaxValue != Timeout)
            {
                Task.Delay(Timeout).ContinueWith(t =>
                {
                    if (queryRunning && Application.Current is EditWindow win && win.Title == dialogTitle)
                    {
                        timedOut = true;
                        Application.Current.Running = false;
                        Application.MainLoop.Driver.Wakeup();
                    }
                });
            }
            
            resetEvent.Wait();
            if (timedOut)
                throw new TimeoutException("User input timed out.");
        }
    }
}
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
                Height = Dim.Fill(2)
            };
            propertiesView.LoadProperties(dataObject);

            propertiesView.Submit += () => { Application.Current.Running = false; };
            
            // Get submit buttons
            var buttons = new List<Button>();
            var submit = members?.FirstOrDefault(m => m.Get<IAccessAnnotation>().IsVisible && m.Get<IMemberAnnotation>()?.Member.GetAttribute<SubmitAttribute>() != null);
            if (submit != null)
            {
                var availableValuesAnnotation = submit.Get<IAvailableValuesAnnotationProxy>();
                foreach (var availableValue in availableValuesAnnotation.AvailableValues)
                {
                    var button = new Button(availableValue.Source.ToString(), availableValuesAnnotation.SelectedValue == availableValue)
                    {
                        Clicked = () =>
                        {
                            Application.Current.Running = false;
                            availableValuesAnnotation.SelectedValue = availableValue;
                            submit.Write();
                        }
                    };
                    buttons.Add(button);
                }
            }
            
            // Create dialog
            // var dialog = new Dialog(getTitle(members, dataObject), buttons.ToArray());
            var dialog = new Dialog(getTitle(members, dataObject));
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
                    if (queryRunning && Application.Current is Dialog)
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
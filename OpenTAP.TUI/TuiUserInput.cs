using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NStack;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class TuiUserInput : IUserInputInterface
    {
        public void RequestUserInput(object dataObject, TimeSpan Timeout, bool modal)
        {
            // Get annotations and all members, including forwarded members.
            var typeData = TypeData.FromType(dataObject.GetType());
            var annotations = AnnotationCollection.Annotate(dataObject);
            var members = annotations.Get<IMembersAnnotation>().Members.ToList();
            members.AddRange(annotations.Get<IForwardedAnnotations>()?.Forwarded ?? new List<AnnotationCollection>());

            // Try to get the title
            var title = members.FirstOrDefault(m => m.Get<DisplayAttribute>()?.Name?.Equals("title", StringComparison.OrdinalIgnoreCase) == true)?.Get<IStringValueAnnotation>()?.Value;
            if (title == null)
                title = members.FirstOrDefault(m => m.Get<DisplayAttribute>()?.Name?.Equals("name", StringComparison.OrdinalIgnoreCase) == true)?.Get<IStringValueAnnotation>()?.Value;
            if (title == null)
                title = typeData.Display?.Name ?? typeData.Name;

            
            StringBuilder message = new StringBuilder();
            var buttons = new List<ustring>();
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            
            foreach (var member in members)
            {
                // Don't show member if not visible
                var memberAccess = member.Get<IAccessAnnotation>();
                if (memberAccess.IsVisible == false)
                    continue;
                
                // If member is read only, only print the value
                var memberValue = member.Get<IStringValueAnnotation>();
                if (memberAccess.IsReadOnly)
                {
                    message.AppendLine($"{member.Get<DisplayAttribute>().Name}: {memberValue.Value}");
                    continue;
                }
                
                // Get available values
                var availableValues = member.Get<IAvailableValuesAnnotationProxy>();
                if (availableValues == null)
                    continue;

                // Add buttons for each available value
                foreach (var value in availableValues.AvailableValues)
                {
                    var stringValue = value.Get<IStringValueAnnotation>();
                    if (stringValue != null)
                        buttons.Add(stringValue.Value);
                }

                // Show query
                var queryRunning = true;
                Application.MainLoop.Invoke(() =>
                {
                    var result = MessageBox.Query(title, message.ToString(), buttons.ToArray());
                    if (result != -1)
                        memberValue.Value = availableValues.AvailableValues.ElementAt(result).Get<IStringValueAnnotation>().Value;
                    
                    // Make sure we only run 1 query at a time
                    resetEvent.Set();
                    queryRunning = false;
                });
                
                // Wait for timeout, then close the dialog
                var timedOut = false;
                Task.Delay(Timeout).ContinueWith(t =>
                {
                    if (queryRunning && Application.Current is Dialog)
                    {
                        timedOut = true;
                        Application.Current.Running = false;
                    }
                });
                
                resetEvent.WaitOne();
                if (timedOut)
                    throw new TimeoutException("User input timed out.");
            }

            // Save changes.
            annotations.Write();
        }
    }
}
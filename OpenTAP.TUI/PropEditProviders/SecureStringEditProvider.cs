using System;
using System.Runtime.InteropServices;
using System.Security;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    /// <summary> Control for editing secure strings. </summary>
    public class SecureStringEditProvider : IPropEditProvider
    {
        string secureStringToString(SecureString secstring) {
            IntPtr strPtr = IntPtr.Zero;
            try 
            {
                strPtr = Marshal.SecureStringToGlobalAllocUnicode(secstring);
                return Marshal.PtrToStringUni(strPtr);
            } 
            finally 
            {
                Marshal.ZeroFreeGlobalAllocUnicode(strPtr);
            }
        }
        public int Order => 0;
        public View Edit(AnnotationCollection annotation)
        {
            var isSecureString = annotation.Get<IReflectionAnnotation>().ReflectionInfo.DescendsTo(typeof(SecureString));
            if (isSecureString == false)
                return null;

            var sec = annotation.Get<IObjectValueAnnotation>().Value as SecureString;
            
            var textField = new TextField(secureStringToString(sec ?? new SecureString())) {Secret = true};
            
            textField.Removed += view => 
            {
                try
                {
                    var sec2 = new SecureString();
                    foreach (var chr in textField.Text)
                    {
                        sec2.AppendChar((char)chr);
                    }

                    annotation.Get<IObjectValueAnnotation>().Value = sec2;
                }
                catch (Exception exception)
                {
                    TUI.Log.Error($"{exception.Message} {DefaultExceptionMessages.DefaultExceptionMessage}");
                    TUI.Log.Debug(exception);
                }
            };
            return textField;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using OpenTap.Tui.Views;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    public class DefaultEditProvider : IPropEditProvider
    {
        public int Order => 1000;

        public View Edit(AnnotationCollection annotation)
        {
            var stredit = annotation.Get<IStringValueAnnotation>();
            if (stredit == null) return null;
            var text = stredit.Value ?? "";
            var view = new View();
            var textField = new TextViewWithEnter(){
                Text = text,
                Height = Dim.Fill(1)
            };
            view.Add(textField);
            textField.ReadOnly = annotation.Get<IAccessAnnotation>()?.IsReadOnly ?? false;
            if (annotation.Get<IEnabledAnnotation>()?.IsEnabled == false)
                textField.ReadOnly = true;
            LayoutAttribute layout = annotation.Get<IMemberAnnotation>()?.Member.GetAttribute<LayoutAttribute>();
            if ((layout?.RowHeight ?? 0) > 1)
            {
                // support multiline edit boxes.
                textField.CloseOnEnter = false;
            }
            
            textField.Closing += () => 
            {
                try
                {
                    stredit.Value = textField.Text.ToString().Replace("\r", "");
                }
                catch (Exception exception)
                {
                    TUI.Log.Error($"{exception.Message} {DefaultExceptionMessages.DefaultExceptionMessage}");
                    TUI.Log.Debug(exception);
                }
            };


            var helperButtons = new HelperButtons()
            {

                Y = Pos.Bottom(textField)
            };
            view.Add(helperButtons);
            helperButtons.SetActions(new List<MenuItem>()
            {
                new MenuItem("Insert file path", "", shortcut: Key.F5, action: () =>
                {
                    FileDialog dialog = new OpenDialog(annotation.Get<DisplayAttribute>()?.Name ?? "...", "") { NameFieldLabel = "Choose file path"};
                    Application.Run(dialog);
                    if (dialog.FilePath ==  null)
                        return;

                    var dialogUri = new Uri(dialog.FilePath.ToString());
                    var workingUri = new Uri(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
                    var path = workingUri.MakeRelativeUri(dialogUri).ToString();
                    textField.InsertText(path);
                    textField.DesiredCursorVisibility = CursorVisibility.Underline;
                })
            }, view); ;
            return view;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTap;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public class EnabledEditProvider : IPropEditProvider
    {
        public int Order => 5;
        public View Edit(AnnotationCollection annotation)
        {
            var members = annotation.Get<IMembersAnnotation>()?.Members.ToArray();
            if (members == null || members.Length != 2)
                return null;
            int enabledIndex = members[0].Get<IMemberAnnotation>().Member.Name == "IsEnabled" ? 0 : 1;
            
            var enabled = members[enabledIndex];
            var value = members[enabledIndex == 0 ? 1 : 0];
            var check = new CheckBox("", (bool)enabled.Get<IObjectValueAnnotation>().Value);
            check.Toggled += (sender, args) => enabled.Get<IObjectValueAnnotation>().Value = check.Checked;
            var viewbox = new View();
            
            var valuebox = PropEditProvider.GetProvider(value, out var _);
            if (valuebox == null) return null;
            viewbox.Add(check);
            valuebox.X = Pos.Right(check);
            valuebox.Width = Dim.Fill();
            viewbox.Add(valuebox);
            return viewbox;
        }
    }
}

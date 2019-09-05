using OpenTap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace OpenTAP.TUI.PropEditProviders
{
    public interface IPropEditProvider
    {
        int Order { get; }
        bool CanEdit(PropertyInfo prop);
        void Edit(PropertyInfo prop, object obj);
    }
}

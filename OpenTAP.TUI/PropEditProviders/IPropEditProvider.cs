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
        /// <summary>
        /// Create the view.
        /// </summary>
        /// <param name="annotation"></param>
        /// <returns></returns>
        View Edit(AnnotationCollection annotation);
    }
}

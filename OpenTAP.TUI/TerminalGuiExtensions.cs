using System;
using System.Collections.Generic;
using System.Text;
using NStack;
using Terminal.Gui;

namespace OpenTAP.TUI
{
    public class ExtendedListView : ListView
    {
        public event EventHandler<EventArgs> OnRedraw;

        public override void Redraw(Rect region)
        {
            base.Redraw(region);
            OnRedraw?.Invoke(this, null);
        }
    }

    public class ExtendedFrameView : FrameView
    {
        public ExtendedFrameView(ustring title) : base(title)
        {
            Subviews[0].Width = Dim.Fill(1);
            Subviews[0].Height = Dim.Fill(1);
        }

        public ExtendedFrameView(Rect frame, ustring title) : base(frame, title)
        {
        }
    }
}

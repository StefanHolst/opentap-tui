using Terminal.Gui;

namespace OpenTap.Tui
{
    public static class ViewExtensions
    {
        static Toplevel getToplevel(View view)
        {
            return view.SuperView != null ? getToplevel(view.SuperView) : (Toplevel)view;
        }
        
        public static bool IsTopActive(this View view)
        {
            var viewTop = getToplevel(view);
            return viewTop == Application.Current;
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using OpenTap.Cli;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public abstract class TuiAction : ICliAction
    {
        public static TraceSource Log = OpenTap.Log.CreateSource("TUI");
        
        /// <summary>  Marks which thread is the main thread. (needed for user input request)</summary>
        public static TapThread MainThread;
        public static CancellationToken CancellationToken;
        public static Toplevel Top { get; set; }
        
        public int Execute(CancellationToken cancellationToken)
        {
            MainThread = TapThread.Current;
            
            CancellationToken = cancellationToken;
            cancellationToken.Register(() =>
            {
                Application.Shutdown();
            });
            
            // Remove console listener to stop any log messages being printed on top of the TUI
            var consoleListener = OpenTap.Log.GetListeners().OfType<ConsoleTraceListener>().FirstOrDefault();
            if (consoleListener != null)
                OpenTap.Log.RemoveListener(consoleListener);

            // Stop OpenTAP from taking over the terminal for user inputs.
            UserInput.SetInterface(null);
            
            // Add tui user input
            UserInput.SetInterface(new TuiUserInput());

            try
            {
                Application.Init();
                Top = Application.Top;
                TuiSettings.Current.LoadSettings();

                return TuiExecute(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(DefaultExceptionMessages.DefaultExceptionMessage);
                Log.Debug(ex);
            }

            return 0;
        }

        public abstract int TuiExecute(CancellationToken cancellationToken);
    }
}
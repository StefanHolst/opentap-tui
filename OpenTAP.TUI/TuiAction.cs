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
        public static ICliAction CurrentAction { get; private set; }
        
        public int Execute(CancellationToken cancellationToken)
        {
            CurrentAction = this;
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
                TuiSettings.Current.LoadSettings();

                return TuiExecute(cancellationToken);
            }
            catch (Exception ex)
            {
                //re-add the console listener to print out the exception
                if (consoleListener != null)
                    OpenTap.Log.AddListener(consoleListener);
                Log.Error(DefaultExceptionMessages.DefaultExceptionMessage);
                Log.Debug(ex);
            }
            finally
            {
                Application.Shutdown();
            }

            return 0;
        }

        public abstract int TuiExecute(CancellationToken cancellationToken);
    }
}
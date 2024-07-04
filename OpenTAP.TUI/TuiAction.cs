using System;
using System.Linq;
using System.Runtime.InteropServices;
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

        #if DEBUG
        const bool isDebug = true;
        #else
        const bool isDebug = false;
        #endif
        
        public static void AssertTuiThread()
        {
            if (isDebug && TapThread.Current != MainThread)
                throw new InvalidOperationException("GUI invoked from outside the GUI thread.");
        }
        
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
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // This fixes an issue causing resizing not to work on Linux. The issue can be reproduced in
                    // the default gui-cs template by running `Console.WriteLine()` before running `Application.Init()`.
                    // I guess dotnet alters the tty state in a way the gui-cs default Linux terminal driver doesn't expect.
                    // Unfortunately, this also breaks the mouse completely.
                    Application.UseSystemConsole = true;
                    Application.Init();
                    // For some reason this is also required.
                    Console.Out.Write(EscSeqUtils.DisableMouseEvents);
                } 
                else 
                {
                    Application.Init();
                }
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

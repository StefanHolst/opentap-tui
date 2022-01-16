using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;

namespace OpenTap.Tui.UnitTest;

public class TuiTester : IDisposable
{
    public FakeDriver driver;
    public FakeMainLoop loop;
    private bool stopped;
    private static TraceSource log = Log.CreateSource(nameof(TuiTester));
        
    public TuiTester()
    {
        driver = new FakeDriver();
        loop = new FakeMainLoop(() => FakeConsole.ReadKey (true));
        Application.Init(driver, loop);

        Task.Run(() =>
        {
            try
            {
                Application.Run();
            }
            catch (Exception e)
            {
                if (stopped)
                    return;

                LogConsole(e);
                throw;
            }
        });
    }
    
    public void Dispose()
    {
        try
        {
            Application.RequestStop();
        }
        finally
        {
            stopped = true;
            Application.Shutdown();
        }
    }

    public static void LogConsole(Exception e)
    {
        if (e != null)
        {
            log.Error(e.Message);
            log.Debug(e);
        }
        
        log.Debug($"Current: {Application.Current}");
        log.Debug($"MostFocused: {Application.Current.MostFocused}");
        
        var content = GetConsoleContent();
        foreach (var line in content)
            log.Debug(line);
        
        log.Flush();
    }

    public static List<string> GetConsoleContent ()
    {
        var content = FakeConsole.Get();
        var _content = new List<string>();

        for (int r = 0; r < FakeConsole.WindowHeight; r++) {
            string line = "";
            for (int c = 0; c < FakeConsole.WindowWidth; c++) {
                if (FakeConsole.CursorLeft == c && FakeConsole.CursorTop == r)
                    line += "#";
                else
                    line += content[c, r];
            }
            _content.Add (line);
        }

        return _content;
    }
    
    public void Wait(Func<bool> method, int timeout = 5, string message = "")
    {
        var now = DateTime.Now;
        var timeoutSpan = TimeSpan.FromSeconds(timeout);

        while (method() == false)
        {
            Thread.Sleep(10);
            if (DateTime.Now - now > timeoutSpan)
            {
                LogConsole(new Exception(message));
                throw new TimeoutException();
            }
        }
    }

    public void WaitIteration()
    {
        var reset = new ManualResetEvent(false);

        void setReset()
        {
            reset.Set();
        }

        Application.Iteration += setReset;
        reset.WaitOne();
        Application.Iteration -= setReset;
    }
}
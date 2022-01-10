using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;

[assembly: CollectionBehavior (DisableTestParallelization = true)]

namespace OpenTap.Tui.UnitTest;

public class ApplicationTest : IDisposable
{
    public FakeDriver driver;
    public FakeMainLoop loop;
    // public Application.RunState runState;
    private bool stopped;
    private TraceSource log = Log.CreateSource(nameof(ApplicationTest));
        
    public ApplicationTest()
    {
        driver = new FakeDriver();
        loop = new FakeMainLoop(() => FakeConsole.ReadKey (true));
        Application.Init(driver, loop);

        // runState = Application.Begin(Application.Top);
        
        Task.Run(() =>
        {
            try
            {
                // Application.RunLoop(runState, false);
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

    public void IsTrue(bool result)
    {
        try
        {
            Assert.True(result);
        }
        catch (Exception e)
        {
            LogConsole(null);
            throw;
        }
    }

    void LogConsole(Exception e)
    {
        if (e != null)
        {
            log.Error(e.Message);
            log.Debug(e);
        }
        
        var content = GetConsoleContent();
        foreach (var line in content)
            log.Debug(line);
        
        log.Flush();
    }

    public List<string> GetConsoleContent ()
    {
        var content = FakeConsole.Get();
        var _content = new List<string>();
			
        for (int r = 0; r < FakeConsole.WindowHeight; r++) {
            string line = "";
            for (int c = 0; c < FakeConsole.WindowWidth; c++) {
                line += content[c, r];
            }
            _content.Add (line);
        }

        return _content;
    }
    
    public void Dispose()
    {
        try
        {
            // Application.End(runState);
            Application.RequestStop();
        }
        finally
        {
            stopped = true;
            Application.Shutdown();
        }
    }
    
    public void Wait(Func<bool> method, int timeout = 5)
    {
        var now = DateTime.Now;
        var timeoutSpan = TimeSpan.FromSeconds(timeout);

        while (method() == false && DateTime.Now - now < timeoutSpan)
            Thread.Sleep(10);

        if (DateTime.Now - now > timeoutSpan)
            throw new TimeoutException();
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
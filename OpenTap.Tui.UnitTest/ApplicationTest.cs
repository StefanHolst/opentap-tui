using System;
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
    private bool stopped;
    private TraceSource log = Log.CreateSource(nameof(ApplicationTest));
        
    public ApplicationTest()
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
                
                var content = driver.GetContent();
                int rows = content.GetLength(0);
                int cols = content.GetLength(1);
                for (int r = 0; r < rows; r++)
                {
                    string line = "";
                    for (int c = 0; c < cols; c++)
                    {
                        line += content[r, c];
                    }
                    log.Debug(line);
                }

                log.Error(e.Message);
                log.Debug(e);
                log.Flush();
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
    
    public void Wait(Func<bool> method, int timeout = 1)
    {
        var now = DateTime.Now;
        var timeoutSpan = TimeSpan.FromSeconds(timeout);

        while (method() == false && DateTime.Now - now < timeoutSpan)
            Thread.Sleep(10);

        if (DateTime.Now - now > timeoutSpan)
            throw new TimeoutException();
    }
}
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
        
    public ApplicationTest()
    {
        driver = new FakeDriver();
        loop = new FakeMainLoop(() => FakeConsole.ReadKey (true));
        Application.Init(driver, loop);

        Task.Run(() =>
        {
            Application.Run();
        });
    }
        
    public void Dispose()
    {
        loop.running = false;
        Application.Shutdown();
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
using System;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTap.Plugins.BasicSteps;
using OpenTap.Tui.Views;
using OpenTap.Tui.Windows;
using Terminal.Gui;
using Xunit;

namespace OpenTap.Tui.UnitTest;

public class TestPlanViewTest : ApplicationTest
{
    [Fact]
    public void Move_Step()
    {
        var plan = new TestPlan();
        plan.Steps.Add(new DelayStep()
        {
            Name = "Delay 1"
        });
        plan.Steps.Add(new DelayStep()
        {
            Name = "Delay 2"
        });
        plan.Save("testing.TapPlan");
        
        var testPlanView = new TestPlanView();
        testPlanView.LoadTestPlan("testing.TapPlan");

        // Add to top and start application
        Application.Top.Add(testPlanView);
        testPlanView.SetFocus();
        
        Assert.True(testPlanView.Plan.ChildTestSteps[0].Name == "Delay 1", "Something is wrong");
        
        // Move step
        Application.MainLoop.Invoke(() => driver.SendKeys(' ', ConsoleKey.Spacebar, false,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys(' ', ConsoleKey.Spacebar, false,false,false));
        Wait(() => testPlanView.Plan.ChildTestSteps[0].Name == "Delay 2");
        
        Assert.True(testPlanView.Plan.ChildTestSteps[0].Name == "Delay 2", "Something is wrong");
    }
    [Fact]
    public void Inject_Step()
    {
        var plan = new TestPlan();
        plan.Steps.Add(new DelayStep());
        plan.Steps.Add(new RepeatStep());
        plan.Save("testing.TapPlan");
        
        var testPlanView = new TestPlanView();
        testPlanView.LoadTestPlan("testing.TapPlan");

        // Add to top and start application
        Application.Top.Add(testPlanView);
        testPlanView.SetFocus();
        
        Assert.True(testPlanView.Plan.ChildTestSteps.FirstOrDefault() is DelayStep);
        
        // Move and inject step
        Application.MainLoop.Invoke(() => driver.SendKeys(' ', ConsoleKey.Spacebar, false,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.RightArrow, ConsoleKey.RightArrow, false,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys(' ', ConsoleKey.Spacebar, false,false,false));
        Wait(() => testPlanView.Plan.ChildTestSteps.FirstOrDefault() is RepeatStep);
        
        Assert.True(testPlanView.Plan.ChildTestSteps.FirstOrDefault() is RepeatStep);
        Assert.True(testPlanView.Plan.ChildTestSteps[0].ChildTestSteps.FirstOrDefault() is DelayStep);
    }
    [Fact]
    public void Copy_Paste_Step()
    {
        var plan = new TestPlan();
        plan.Steps.Add(new DelayStep());
        plan.Save("testing.TapPlan");
        
        var testPlanView = new TestPlanView();
        testPlanView.LoadTestPlan("testing.TapPlan");

        // Add to top and start application
        Application.Top.Add(testPlanView);
        testPlanView.SetFocus();
        
        Assert.True(testPlanView.Plan.ChildTestSteps.Count == 1);
        
        // Send keys
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.C, ConsoleKey.C, true,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.V, ConsoleKey.V, true,false,false));
        Wait(() => testPlanView.Plan.ChildTestSteps.Count == 2);
        
        Assert.True(testPlanView.Plan.ChildTestSteps.Count == 2);
    }
    [Fact]
    public void Delete_Step()
    {
        var plan = new TestPlan();
        plan.Steps.Add(new DelayStep());
        plan.Steps.Add(new DelayStep());
        plan.Save("testing.TapPlan");
        
        var testPlanView = new TestPlanView();
        testPlanView.LoadTestPlan("testing.TapPlan");

        // Add to top and start application
        Application.Top.Add(testPlanView);
        testPlanView.SetFocus();
        
        Assert.True(testPlanView.Plan.ChildTestSteps.Count == 2);
        
        // Send keys
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.Delete, ConsoleKey.Delete, false,false,false));
        Wait(() => testPlanView.Plan.ChildTestSteps.Count == 1);
        
        Assert.True(testPlanView.Plan.ChildTestSteps.Count == 1);
    }
    [Fact]
    public void Add_Step()
    {
        var plan = new TestPlan();
        plan.Steps.Add(new DelayStep());
        plan.Save("testing.TapPlan");
        
        var testPlanView = new TestPlanView();
        testPlanView.LoadTestPlan("testing.TapPlan");

        // Add to top and start application
        Application.Top.Add(testPlanView);
        testPlanView.SetFocus();

        Assert.True(testPlanView.Plan.ChildTestSteps.Count == 1);

        // Send keys
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.T, ConsoleKey.T, false, false, true));
        Wait(() => Application.Current is NewPluginWindow);
        
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.RightArrow, ConsoleKey.RightArrow, false,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        Wait(() => testPlanView.Plan.ChildTestSteps.Count == 2);
        
        Assert.True(testPlanView.Plan.ChildTestSteps.Count == 2);
    }
    [Fact]
    public void Insert_Step()
    {
        var plan = new TestPlan();
        plan.Steps.Add(new RepeatStep());
        plan.Save("testing.TapPlan");
        
        var testPlanView = new TestPlanView();
        testPlanView.LoadTestPlan("testing.TapPlan");

        // Add to top and start application
        Application.Top.Add(testPlanView);
        testPlanView.SetFocus();

        Assert.True(testPlanView.Plan.ChildTestSteps.Count == 1);
        Assert.True(testPlanView.Plan.ChildTestSteps[0].ChildTestSteps.Count == 0);

        // Send keys
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.T, ConsoleKey.T, true, false, true));
        Wait(() => Application.Current is NewPluginWindow);
        
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.RightArrow, ConsoleKey.RightArrow, false,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        Wait(() => testPlanView.Plan.ChildTestSteps[0].ChildTestSteps.Count == 1);
        
        Assert.True(testPlanView.Plan.ChildTestSteps[0].ChildTestSteps.Count == 1);
    }
    [Fact(Skip = "")]
    public void Save_Plan()
    {
        var plan = new TestPlan();
        plan.Steps.Add(new RepeatStep());
        plan.Save("testing.TapPlan");

        var planSize = new FileInfo("testing.TapPlan").Length;
            
        var testPlanView = new TestPlanView();
        testPlanView.LoadTestPlan("testing.TapPlan");
        testPlanView.Plan.ChildTestSteps.Add(new DelayStep());

        // Add to top and start application
        Application.Top.Add(testPlanView);
        testPlanView.SetFocus();

        // Send keys
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.S, ConsoleKey.S, false, false, true));
        Wait(() => planSize != new FileInfo("testing.TapPlan").Length);
        
        Assert.True(planSize != new FileInfo("testing.TapPlan").Length);
    }
    [Fact(Skip = "")]
    public void Save_As_Plan()
    {
        if (File.Exists("testing1.TapPlan"))
        {
            File.Delete("testing1.TapPlan");
            Thread.Sleep(100);
        }

        var testPlanView = new TestPlanView();
        
        // Add to top and start application
        Application.Top.Add(testPlanView);
        testPlanView.SetFocus();

        // Send keys
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.S, ConsoleKey.S, true, false, true));
        Wait(() => Application.Current is SaveDialog);
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.Tab, ConsoleKey.Tab, true, false, false));
        Wait(() => Application.Current.MostFocused is TextField);
        var textField = Application.Current.MostFocused as TextField;
        textField.Text = "testing1.TapPlan";
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false, false, false));
        Wait(() => Application.Current is Toplevel);
        
        Thread.Sleep(100);
        Assert.True(File.Exists("testing1.TapPlan"));
    }
    [Fact(Skip = "")]
    public void Open_Plan()
    {
        var plan = new TestPlan();
        plan.Steps.Add(new RepeatStep());
        plan.Save("testing.TapPlan");
        
        var testPlanView = new TestPlanView();

        // Add to top and start application
        Application.Top.Add(testPlanView);
        testPlanView.SetFocus();

        // Send keys
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.O, ConsoleKey.O, false, false, true));
        Wait(() => Application.Current is OpenDialog);
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.Tab, ConsoleKey.Tab, true, false, false));
        Wait(() => Application.Current.MostFocused is TextField);
        var textField = Application.Current.MostFocused as TextField;
        textField.Text = "testing.TapPlan";
        Application.MainLoop.Invoke(() => driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false, false, false));
        Wait(() => testPlanView.Plan.ChildTestSteps.Any());
        
        Assert.True(testPlanView.Plan.ChildTestSteps.Any());
    }
}

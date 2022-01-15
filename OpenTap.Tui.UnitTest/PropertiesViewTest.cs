using System;
using System.Linq;
using OpenTap.Tui.Views;
using OpenTap.UnitTest;
using Terminal.Gui;

namespace OpenTap.Tui.UnitTest;

// public class PropertiesViewTest : ApplicationTest, ITestFixture
public class PropertiesViewTest : ITestFixture
{
    [Test]
    public void Load_Delay()
    {
        using var test = new TuiTester();
        var plan = new TestPlan();
        plan.Steps.Add(new UnitTestStep());

        var propView = new PropertiesView()
        {
            Width = 50,
            Height = 100
        };
        propView.LoadProperties(plan.Steps[0]);
            
        // Add to top and start application
        Application.Top.Add(propView);
        propView.SetFocus();

        test.WaitIteration();

        var content = TuiTester.GetConsoleContent();
        Assert.Contains(content, s => s.Contains(nameof(UnitTestStep.StringType)));
    }

    [Test]
    public void Edit_Prop()
    {
        using var test = new TuiTester();
        var plan = new TestPlan();
        plan.Steps.Add(new UnitTestStep());

        var propView = new PropertiesView();
        propView.LoadProperties(plan.Steps[0]);
            
        // Add to top and start application
        Application.Top.Add(propView);
        propView.SetFocus();

        Application.MainLoop.Invoke(() => test?.driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        test.WaitIteration();

        Assert.True(Application.Current.Subviews.FirstOrDefault()?.Subviews.FirstOrDefault() is TextViewWithEnter);
    }
}

public class UnitTestStep : TestStep
{
    public string StringType { get; set; }
    
    public override void Run()
    {
    }
}
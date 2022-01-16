using System;
using System.Collections.Generic;
using System.Linq;
using OpenTap.Tui.Views;
using OpenTap.UnitTest;
using Terminal.Gui;

namespace OpenTap.Tui.UnitTest;

public class PropertiesViewTest : ITestFixture
{
    [Test]
    public void Load_Prop()
    {
        using var tester = new TuiTester();
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

        tester.WaitIteration();

        var content = TuiTester.GetConsoleContent();
        Assert.Contains(content, s => s.Contains(nameof(UnitTestStep.StringType)));
    }

    [Test]
    public void Edit_Prop()
    {
        using var tester = new TuiTester();
        var plan = new TestPlan();
        plan.Steps.Add(new UnitTestStep());

        var propView = new PropertiesView();
        propView.LoadProperties(plan.Steps[0]);

        // Add to top and start application
        Application.Top.Add(propView);
        propView.SetFocus();

        // Edit string
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        tester.WaitIteration();
        Assert.True(Application.Current.MostFocused is TextViewWithEnter);
        
        // Edit bool
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Escape, ConsoleKey.Escape, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        tester.WaitIteration();
        Assert.True(Application.Current.MostFocused is CheckBox);
        
        // Edit List
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Escape, ConsoleKey.Escape, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        tester.WaitIteration();
        Assert.True(Application.Current.MostFocused is TableView);
        
        // Edit Enum
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Escape, ConsoleKey.Escape, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        tester.WaitIteration();
        Assert.True(Application.Current.MostFocused is ListView);
        
        // Edit FilePath
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Escape, ConsoleKey.Escape, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        tester.WaitIteration();
        Assert.True(Application.Current.MostFocused.SuperView.SuperView is FileDialog);
        
        // Edit Action
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Escape, ConsoleKey.Escape, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        tester.WaitIteration();
        Assert.True(Application.Current.MostFocused.SuperView.SuperView is Dialog);
    }
}

public class UnitTestStep : TestStep
{
    public string StringType { get; set; }
    public bool BoolType { get; set; }
    public List<string> ListType { get; set; }
    public Verdict EnumType { get; set; }
    [FilePath]
    public string FilePathType { get; set; } = "";

    public Action ActionType { get; set; } = () => { };

    public override void Run()
    {
    }
}
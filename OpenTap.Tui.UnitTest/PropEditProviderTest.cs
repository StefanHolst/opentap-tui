using System;
using System.Linq;
using OpenTap.Tui.PropEditProviders;
using OpenTap.Tui.Views;
using OpenTap.UnitTest;
using Terminal.Gui;

namespace OpenTap.Tui.UnitTest;

public class PropEditProviderTest : ITestFixture
{
    public class TypeTest
    {
        public Action MethodType => () => {};
        public bool BooleanType { get; set; }
        public ColorSchemeViewmodel ColorSchemeViewmodelType { get; set; } = new ColorSchemeViewmodel();
    }

    [Test]
    public void ActionProviderTest()
    {
        using var tester = new TuiTester();
      
        var member = AnnotationCollection.Annotate(new TypeTest()).Get<IMembersAnnotation>().Members.FirstOrDefault(m => m.Get<IObjectValueAnnotation>().Value is Action);
        var propEditor = PropEditProvider.GetProvider(member, out var provider);
        
        Assert.True(provider is ActionProvider);
    }

    [Test]
    public void BooleanEditProviderTest()
    {
        using var tester = new TuiTester();
        var member = AnnotationCollection.Annotate(new TypeTest()).Get<IMembersAnnotation>().Members.FirstOrDefault(m => m.Get<IObjectValueAnnotation>().Value is bool);
        var propEditor = PropEditProvider.GetProvider(member, out var provider);
        
        Assert.True(provider is BooleanEditProvider);
        
        var value = (bool) member.Get<IObjectValueAnnotation>().Value;
        Assert.True(value == false);
        
        // Add to top and start application
        Application.Top.Add(propEditor);
        propEditor.SetFocus();
        tester.WaitIteration();

        
        // Set a new value
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Spacebar, ConsoleKey.Spacebar, false,false,false));
        tester.WaitIteration();
        
        // Save values to reference object
        member.Write();
        member.Read();
        
        value = (bool) member.Get<IObjectValueAnnotation>().Value;
        Assert.True(value == true);
    }

    [Test]
    public void ColorSchemeEditProviderTest()
    {
        using var tester = new TuiTester();
        var member = AnnotationCollection.Annotate(new TypeTest()).Get<IMembersAnnotation>().Members.FirstOrDefault(m => m.Get<IObjectValueAnnotation>().Value is ColorSchemeViewmodel);
        var propEditor = PropEditProvider.GetProvider(member, out var provider);
        
        Assert.True(provider is ColorSchemeEditProvider);
        
        var value = (ColorSchemeViewmodel) member.Get<IObjectValueAnnotation>().Value;
        Assert.True(value.NormalForeground == Color.Black);
        
        // Add to top and start application
        Application.Top.Add(propEditor);
        propEditor.SetFocus();
        tester.WaitIteration();

        // Set a new value
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.DownArrow, ConsoleKey.DownArrow, false,false,false));
        tester.WaitIteration();
        // tester.WaitIteration();
        Application.MainLoop.Invoke(() => tester.driver.SendKeys((char)ConsoleKey.Enter, ConsoleKey.Enter, false,false,false));
        tester.Wait(() => Application.Current.Focused is PropertiesView);
        
        // Save values to reference object
        member.Write();
        member.Read();
        
        value = (ColorSchemeViewmodel) member.Get<IObjectValueAnnotation>().Value;
        Assert.True(value.NormalForeground == Color.Blue);
    }
}
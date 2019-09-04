using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using OpenTap;
using Terminal.Gui;

public class ResourceSettingsView : Window
{
    private List<IResource> resources { get; set; }
    private Type resourceType { get; set; }
    private List<string> list { get; set; }
    private ListView listView { get; set; }
    private PropertiesView detailsView { get; set; } = new PropertiesView();

    public ResourceSettingsView(List<IResource> resources, Type resourceType, string title) : base(null)
    {
        this.resources = resources;
        this.resourceType = resourceType;

        // list frame
        var frame = new FrameView(title)
        {
            Width = Dim.Percent(25),    
            Height = Dim.Fill()
        };

        // resource list
        list = resources.Select(r => r.Name).ToList();
        listView = new ListView(list)
        {
            Height = Dim.Fill(1)
        };
        listView.SelectedChanged += () => 
        {
            var test = resources[listView.SelectedItem];
            detailsView.LoadProperties(test);
        };
        frame.Add(listView);

        // add resource button
        var button = new Button("+")
        {
            Width = Dim.Fill(),
            Y = Pos.Bottom(listView)
        };
        button.Clicked += () => 
        {
            var newPlugin = new NewPluginView(resourceType, title);
            Application.Run(newPlugin);
            if (newPlugin.PluginType != null)
            {
                resources.Add(Activator.CreateInstance(newPlugin.PluginType) as IResource);
                listView.SetSource(resources.Select(r => r.Name).ToList());
            }
        };
        frame.Add(button);

        // details frame
        var detailFrame = new FrameView("Details")
        {
            X = Pos.Percent(25),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        detailFrame.Add(detailsView);

        Add(frame);
        Add(detailFrame);
    }

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        if (keyEvent.Key == Key.Esc)
        {
            Running = false;
            return true;
        }

        return base.ProcessKey(keyEvent);
    }
}
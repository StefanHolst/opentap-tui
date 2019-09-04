using System.ComponentModel;
using System.Reflection;
using Terminal.Gui;

public class PropEditView : Window
{
    public object Value { get; set; }
    private TextField textField { get; set; }
    private object Input { get; set; }

    public PropEditView(PropertyInfo prop, object input) : base(prop.Name)
    {
        Input = input;
        textField = new TextField(input.ToString());
        Add(textField);
    }

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        if (keyEvent.Key == Key.Esc)
        {
            Running = false;
            return true;
        }

        if (keyEvent.Key == Key.Enter)
        {
            try
            {
                Value = TypeDescriptor.GetConverter(Input).ConvertFrom(textField.Text.ToString());
            }
            catch { }
            Running = false;
            return true;
        }

        return base.ProcessKey(keyEvent);
    }
}
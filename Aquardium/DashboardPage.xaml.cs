using System.Collections.ObjectModel;

namespace Aquardium;

public partial class DashboardPage : TabbedPage
{
    public ObservableCollection<ArduinoDevice> ArduinoList { get; set; }
    public ArduinoDevice SelectedArduino { get; set; }

    public DashboardPage()
    {
        InitializeComponent();
        ArduinoList = App.ArduinoList;
        BindingContext = this;

        if (ArduinoList.Count > 0)
        {
            SelectedArduino = ArduinoList.First();
        }
        else DisplayAlert("Error", "ArduinoList is empty!", "OK");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        string allArduinos = "";
        string allShellItems = "";
        foreach (var ard in App.ArduinoList)
        {
            allArduinos += ard.Name + ", ";
        }
        if (Shell.Current != null)

            foreach (var sh in Shell.Current.Items)
            {
                allArduinos += sh.Title + ", ";
            }
        DisplayAlert("ArduinoList", allArduinos);
        DisplayAlert("ShellItems", allShellItems);
    }

    public void DisplayAlert(string title, string message)  //FOR TESTING
    {
        DisplayAlert(title, message, "OK");
    }

    public void UpdateStatus(string message)
    {
        StatusValue.Text = message;
    }

    public void UpdateTemperature(string message)
    {
        TemperatureValue.Text = message + " °C";
    }

    private void SendControlCommand(object sender, EventArgs e)
    {
        /*if (SelectedArduino != null)
        {
            DisplayAlert("Command Sent", $"Control command sent to {SelectedArduino.Name}", "OK");
        }
        else
        {
            DisplayAlert("Error", "No Arduino selected", "OK");
        }*/
        string allArduinos = "";
        string allShellItems = "";
        foreach (var ard in App.ArduinoList)
        {
            allArduinos += ard.Name + ", ";
        }
        if (Shell.Current != null)

            foreach (var sh in Shell.Current.Items)
            {
                allArduinos += sh.Title + ", ";
            }
        AppShell.CurrentDashboardPage.DisplayAlert("ArduinoList", allArduinos);
        AppShell.CurrentDashboardPage.DisplayAlert("ShellItems", allShellItems);
    }

    private void OnSimulateConnectionClicked(object sender, EventArgs e)
    {
        var mqttService = new MqttService();
        mqttService.SimulateArduinoConnection("test-arduino", "test-arduino");
    }
}

public class ArduinoDevice
{
    public string Name { get; set; }
    public string Id { get; set; }
}
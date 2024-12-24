using System.Collections.ObjectModel;

namespace Aquardium;

public partial class DashboardPage : TabbedPage
{
    public ObservableCollection<ArduinoDevice> ArduinoList { get; set; }
    public ArduinoDevice SelectedArduino { get; set; }
    private string allArduinos = "";

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

        if (Shell.Current != null)
            foreach (var sh in Shell.Current.Items)
                allArduinos += sh.Title + ", ";

        DisplayAlert("ShellItems", allArduinos);
    }

    public void DisplayAlert(string title, string message)  //FOR TESTING
    {
        DisplayAlert(title, message, "OK");
    }

    protected override async void OnAppearing()     //FOR TESTING
    {
        base.OnAppearing();
        string flyoutItems = string.Join(", ", Shell.Current.Items.Select(i => i.Title));
        await Application.Current.MainPage.DisplayAlert("Flyout Items", flyoutItems, "OK");
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
        if (SelectedArduino != null)
        {
            DisplayAlert("Command Sent", $"Control command sent to {SelectedArduino.Name}", "OK");
        }
        else
        {
            DisplayAlert("Error", "No Arduino selected", "OK");
        }
    }
}

public class ArduinoDevice
{
    public string Name { get; set; }
    public string Id { get; set; }
}
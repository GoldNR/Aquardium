using CommunityToolkit.Mvvm.Messaging;

namespace Aquardium;

public partial class ArduinoTabbedPage : TabbedPage
{
    private ArduinoDevice Device { get; set; }

    public ArduinoTabbedPage(ArduinoDevice device)
	{
		InitializeComponent();
        Device = device;
        BindingContext = Device;
        Title = device.Id;

        if (Device.Status == "Online" || Device.Status == "Connected")
            StatusValue.TextColor = Colors.Green;

        else if (Device.Status == "Offline" || Device.Status == "Disconnected")
            StatusValue.TextColor = Colors.Red;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        WeakReferenceMessenger.Default.Register<StatusUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
                StatusValue.Text = message.Value.Status;

            if (Device.Status == "Online" || Device.Status == "Connected")
                StatusValue.TextColor = Colors.Green;

            else if (Device.Status == "Offline" || Device.Status == "Disconnected")
                StatusValue.TextColor = Colors.Red;
        });
        WeakReferenceMessenger.Default.Register<TemperatureUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
                TemperatureValue.Text = message.Value.Temperature == "-127.00 °C" ? "Sensor disconnected" : $"{message.Value.Temperature} °C";
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        WeakReferenceMessenger.Default.Unregister<StatusUpdateMessage>(this);
        WeakReferenceMessenger.Default.Unregister<TemperatureUpdateMessage>(this);
    }

    private void OnSimulateArduino(object sender, EventArgs e)      // For simulating arduino
    {
        if (Application.Current.MainPage is MainPage mainPage)
        {
            var arduino = new ArduinoDevice
            {
                Id = "Simulated Arduino",
                Status = "Online"
            };
            mainPage.Devices.Add(arduino);
            mainPage.Detail = new NavigationPage(new ArduinoTabbedPage(arduino));
        }
    }
}
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
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StatusValue.Text = Device.Status;
        if (Device.Status == "online")
            StatusValue.TextColor = Colors.Green;

        else if (Device.Status == "offline")
            StatusValue.TextColor = Colors.Red;

        WeakReferenceMessenger.Default.Register<TemperatureUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
                TemperatureValue.Text = $"{message.Value.Temperature} °C";
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        WeakReferenceMessenger.Default.Unregister<TemperatureUpdateMessage>(this);
    }
}
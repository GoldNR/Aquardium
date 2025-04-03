using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Platform;
using Plugin.BLE.Abstractions.Contracts;
using System.Runtime.CompilerServices;

namespace Aquardium;

public partial class ArduinoTabbedPage : TabbedPage
{
    private ArduinoDevice Device { get; set; }
    private string connectionMode;
    private IDevice device;

    public ArduinoTabbedPage(ArduinoDevice device, string connectionMode)
	{
		InitializeComponent();
        Device = device;
        BindingContext = Device;
        Title = device.Id;
        this.connectionMode = connectionMode;

        if (Device.Status == "Online" || Device.Status == "Connected")
            StatusValue.TextColor = Colors.Green;

        else if (Device.Status == "Offline" || Device.Status == "Disconnected")
            StatusValue.TextColor = Colors.Red;
    }

    public ArduinoTabbedPage(ArduinoDevice device, string connectionMode, ref IDevice arduino_i)
    {
        InitializeComponent();
        Device = device;
        BindingContext = Device;
        Title = device.Id;
        this.connectionMode = connectionMode;
        this.device = arduino_i;

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
                TemperatureValue.Text = message.Value.Temperature == "-127.00" ? "Sensor disconnected" : $"{message.Value.Temperature} °C";
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        WeakReferenceMessenger.Default.Unregister<StatusUpdateMessage>(this);
        WeakReferenceMessenger.Default.Unregister<TemperatureUpdateMessage>(this);
    }

    private void HourEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!int.TryParse(e.NewTextValue, out int value) || value < 0 || value > 23)
        {
            ((Entry)sender).Text = "";
        }
    }

    private void MinuteEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!int.TryParse(e.NewTextValue, out int value) || value < 0 || value > 59)
        {
            ((Entry)sender).Text = "";
        }
    }

    private async void OnSaveFeederTime(object sender, EventArgs e)
    {
        int hour = int.Parse(HourEntry.Text), minute = int.Parse(MinuteEntry.Text);
        if (hour >= 0 && hour <= 23 &&
            minute >= 0 && minute <= 59)
        {
            bool isConfirmed = await DisplayAlert("Confirm", $"Set feeder time to {hour:D2}:{minute:D2}?", "Yes", "Cancel");

            if (isConfirmed)
            {
                String message = "{\"id\":\"" + $"{Device.Id}" + "\",\"hour\":\"" + $"{hour:D2}" + "\",\"minute\":\"" + $"{minute:D2}" + "\"}";
                if (connectionMode == "WIFI")
                    await MqttService.PublishMessageAsync(message, $"{Device.Id}/servo");

                else if (connectionMode == "BLUETOOTH")
                    await BluetoothService.SendMessageAsync(message, device, "00002a5c-0000-1000-8000-00805f9b34fb");

                await DisplayAlert("Success", $"Feeder time set to {hour:D2}:{minute:D2}", "OK");
            }
        }
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
            mainPage.Detail = new NavigationPage(new ArduinoTabbedPage(arduino, "TEST"));
        }
    }
}
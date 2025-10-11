using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Platform;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Aquardium;

public partial class ArduinoTabbedPage : TabbedPage
{
    private ArduinoDevice Device { get; set; }
    private string connectionMode;

    public ArduinoTabbedPage(ArduinoDevice device, string connectionMode)
	{
		InitializeComponent();
        Device = device;
        BindingContext = Device;
        SetPageTitleAsync();
        this.connectionMode = connectionMode;

        if (connectionMode == "BLUETOOTH")
        {
            var wifiSetupButton = new Button
            {
                Text = "WiFi Setup",
                BackgroundColor = Color.FromArgb("#2196F3"),
                HeightRequest = 100
            };
            Grid.SetRow(wifiSetupButton, 2);
            Grid.SetColumn(wifiSetupButton, 0);
            wifiSetupButton.Clicked += OnWifiSetupClicked;
            controlButtonGrid.Children.Add(wifiSetupButton);
        }
    }

    private async Task SetPageTitleAsync()
    {
        var deviceNames = await DeviceNameStorage.LoadAsync();

        if (deviceNames.TryGetValue(Device.Id, out var customName))
            Title = customName + $" ({Device.Id})";

        else
            Title = Device.Id;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        WeakReferenceMessenger.Default.Register<StatusUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
            {
                StatusValue.Text = message.Value.Status;

                if (Device.Status == "Online" || Device.Status == "Connected")
                    StatusValue.TextColor = Colors.Green;

                else if (Device.Status == "Offline" || Device.Status == "Disconnected")
                    StatusValue.TextColor = Colors.Red;
            }
        });
        WeakReferenceMessenger.Default.Register<TemperatureUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
                TemperatureValue.Text = message.Value.Temperature == "-127.00" ? "Sensor disconnected" : $"{message.Value.Temperature} °C";
        });
        WeakReferenceMessenger.Default.Register<TurbidityUpdateMessage>(this, (recipient, message) =>
        {
            int sensorValue = int.Parse(message.Value.Turbidity);
            if (sensorValue <= 1023 && sensorValue >= 800)
            {
                TurbidityValue.Text = "Clear";
                TurbidityValue.TextColor = Colors.Green;
            }
            else if (sensorValue <= 799 && sensorValue >= 600)
            {
                TurbidityValue.Text = "Mildly Cloudy";
                TurbidityValue.TextColor = Colors.Yellow;
            }
            else if (sensorValue <= 599 && sensorValue >= 300)
            {
                TurbidityValue.Text = "Cloudy";
                TurbidityValue.TextColor = Colors.Orange;
            }
            else if (sensorValue <= 299 && sensorValue >= 0)
            {
                TurbidityValue.Text = "Very Cloudy";
                TurbidityValue.TextColor = Colors.Red;
            }
            else
            {
                TurbidityValue.Text = "Sensor disconnected";
                TurbidityValue.TextColor = Colors.Gray;
            }
        });
        WeakReferenceMessenger.Default.Register<pHUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
                pHValue.Text = message.Value.pH == "-1.00" ? "Sensor disconnected" : $"{message.Value.pH}";
        });
        WeakReferenceMessenger.Default.Register<TimeLastFedUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
            {
                TimeValue.Text = message.Value.TLF == "0/0/0 0:0" ? "RTC Module disconnected" : $"{message.Value.TLF}";
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        WeakReferenceMessenger.Default.Unregister<StatusUpdateMessage>(this);
        WeakReferenceMessenger.Default.Unregister<TemperatureUpdateMessage>(this);
        WeakReferenceMessenger.Default.Unregister<TurbidityUpdateMessage>(this);
        WeakReferenceMessenger.Default.Unregister<pHUpdateMessage>(this);
        WeakReferenceMessenger.Default.Unregister<TimeLastFedUpdateMessage>(this);
    }

    private async void OnSetCustomNameClicked(object sender, EventArgs e)
    {
        var name = await this.ShowPopupAsync<string>(new SetCustomNamePopup());
        if (name != null)
        {
            Title = name.Result + $" ({Device.Id})" ?? Device.Id;
            var deviceNames = await DeviceNameStorage.LoadAsync();
            deviceNames[Device.Id] = name.Result;
            await DeviceNameStorage.SaveAsync(deviceNames);
        }
    }

    private async void OnSetFeederTimeClicked(object sender, EventArgs e) 
    {
        var jsonResult = await this.ShowPopupAsync<string>(new FeederTimePopup());
        if (jsonResult.Result != null)
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonResult.Result);
            int hour = result["Hour"];
            int minute = result["Minute"];

            bool isConfirmed = await DisplayAlert("Confirm", $"Set feeder time to {hour:D2}:{minute:D2}?", "Yes", "Cancel");

            if (isConfirmed)
            {
                String message = "{\"hour\":\"" + $"{hour:D2}" + "\",\"minute\":\"" + $"{minute:D2}" + "\"}";

                if (connectionMode == "WIFI")
                    await MqttService.PublishMessageAsync(message, $"{Device.Id}/servo");

                else if (connectionMode == "BLUETOOTH")
                    await BluetoothService.SendMessageAsync(Device.Id, message, "12345678-1234-5678-1234-56789abcdef3");

                await DisplayAlert("Success", $"Feeder time set to {hour:D2}:{minute:D2}", "OK");
            }
        }
    }

    private async void OnFeedNowClicked(object sender, EventArgs e)
    {
        bool isConfirmed = await DisplayAlert("Confirm", "Are you sure to feed now?", "Yes", "Cancel");
        
        if (isConfirmed) 
        {
            String message = " ";
            if (connectionMode == "WIFI")
                await MqttService.PublishMessageAsync(message, $"{Device.Id}/now");

            else if (connectionMode == "BLUETOOTH")
                await BluetoothService.SendMessageAsync(Device.Id, message, "12345678-1234-5678-1234-56789abcdef4");

            await DisplayAlert("Success", "Feeder activated", "OK");
        }
    }

    private async void OnResetArduinoClicked(object sender, EventArgs e)
    {
        bool isConfirmed = await DisplayAlert("Confirm", "Are you sure to reset the Arduino? This will only turn the device off and on, and will NOT affect its current settings. You will be disconnected from the device momentarily", "Yes", "Cancel");

        if (isConfirmed)
        {
            String message = " ";
            if (connectionMode == "WIFI")
                await MqttService.PublishMessageAsync(message, $"{Device.Id}/reset");

            else if (connectionMode == "BLUETOOTH")
                await BluetoothService.SendMessageAsync(Device.Id, message, "12345678-1234-5678-1234-56789abcdef6");

            await DisplayAlert("Success", "Resetting Arduino. Expect disconnection in a moment.", "OK");
        }
    }

    private async void OnWifiSetupClicked(object? sender, EventArgs e)
    {
        var jsonResult = await this.ShowPopupAsync<string>(new WifiSetupPopup());
        if (jsonResult.Result != null)
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonResult.Result);
            string ssid = result["Ssid"];
            string pass = result["Password"];

            bool isConfirmed = await DisplayAlert("Confirm", $"Connect to network \"{ssid}\"?", "Yes", "Cancel");

            if (isConfirmed)
            {
                if (connectionMode == "BLUETOOTH")
                {
                    await BluetoothService.SendMessageAsync(Device.Id, ssid, "12345678-1234-5678-1234-56789abcdef7");
                    await BluetoothService.SendMessageAsync(Device.Id, pass, "12345678-1234-5678-1234-56789abcdef8");

                    await DisplayAlert("Success", "Data sent to the device", "OK");
                }

                
            }
        }
    }

}

public static class DeviceNameStorage
{
    private static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, "deviceNames.json");

    public static async Task<Dictionary<string, string>> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new Dictionary<string, string>();

        var json = await File.ReadAllTextAsync(FilePath);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>();
    }

    public static async Task SaveAsync(Dictionary<string, string> deviceNames)
    {
        var json = JsonSerializer.Serialize(deviceNames, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(FilePath, json);
    }
}
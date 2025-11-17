using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using Plugin.Firebase.CloudMessaging;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Aquardium;

public partial class ArduinoTabbedPage : TabbedPage
{
    private ArduinoDevice Device { get; set; }
    private ConnectionMode Mode { get; set; }
    private static int simCount = 1;

    public ArduinoTabbedPage(ArduinoDevice device, ConnectionMode connectionMode)
	{
		InitializeComponent();
        Device = device;
        BindingContext = Device;
        SetPageTitleAsync();
        this.Mode = connectionMode;

        if (connectionMode == ConnectionMode.Bluetooth)
        {
            var wifiSetupButton = new Button
            {
                Text = "WiFi Setup",
                BackgroundColor = Color.FromArgb("#2196F3"),
                HeightRequest = 100
            };
            Grid.SetRow(wifiSetupButton, 2);
            Grid.SetColumn(wifiSetupButton, 1);
            wifiSetupButton.Clicked += OnWifiSetupClicked;
            controlButtonGrid.Children.Add(wifiSetupButton);
        }

        Device.Type = Preferences.Get("fishType_" + Device.Id, null);
        Device.Quantity = Preferences.Get("fishQuantity_" + Device.Id, null);
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
            {
                TemperatureValue.Text = message.Value.Temperature == "-127.00" ? "Sensor disconnected" : $"{message.Value.Temperature} °C";

                var temp = double.Parse(message.Value.Temperature);
                if (temp <= 18.0d || temp >= 31.0d)
                    TemperatureValue.TextColor = Colors.Red;

                else
                    TemperatureValue.TextColor = Colors.Green;
            }
        });
        WeakReferenceMessenger.Default.Register<TurbidityUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
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
            }
        });
        WeakReferenceMessenger.Default.Register<pHUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
            {
                pHValue.Text = message.Value.pH == "-1.00" ? "Sensor disconnected" : $"{message.Value.pH}";

                double ph = double.Parse(pHValue.Text);
                if (ph < 6 || ph > 8)
                    pHValue.TextColor = Colors.Red;

                else
                    pHValue.TextColor = Colors.Green;
            }
        });
        WeakReferenceMessenger.Default.Register<TimeLastFedUpdateMessage>(this, (recipient, message) =>
        {
            if (message.Value.ArduinoId == Device.Id)
                TimeValue.Text = message.Value.TLF == "0/0/0 0:0" ? "RTC Module disconnected" : $"{message.Value.TLF}";
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
            int[] hours = new int[3] {99, 99, 99};
            int[] minutes = new int[3] {99, 99, 99};
            var result = JsonSerializer.Deserialize<List<Dictionary<string, int>>>(jsonResult.Result);

            for (int i = 0; i < result.Count; i++)
            {
                var dict = result[i];

                if (dict.TryGetValue($"hour{i + 1}", out var hourValue))
                    hours[i] = hourValue;

                if (dict.TryGetValue($"minute{i + 1}", out var minuteValue))
                    minutes[i] = minuteValue;
            }

            bool isConfirmed = await DisplayAlert("Confirm", $"Review the times you set. Are you sure?", "Yes", "Cancel");

            if (isConfirmed)
            {
                String message = "{\"hour1\":\"" + $"{hours[0]}" + "\",\"minute1\":\"" + $"{minutes[0]}" + "\"," + 
                                  "\"hour2\":\"" + $"{hours[1]}" + "\",\"minute2\":\"" + $"{minutes[1]}" + "\"," +
                                  "\"hour3\":\"" + $"{hours[2]}" + "\",\"minute3\":\"" + $"{minutes[2]}" + "\"" + "}";

                if (Mode == ConnectionMode.WiFi)
                    await MqttService.PublishMessageAsync(message, $"{Device.Id}/servo");

                else if (Mode == ConnectionMode.Bluetooth)
                    await BluetoothService.SendMessageAsync(Device.Id, message, "12345678-1234-5678-1234-56789abcdef3");

                await DisplayAlert("Success", $"Feeder time successfully set", "OK");
            }
        }
    }

    private async void OnFeedNowClicked(object sender, EventArgs e)
    {
        bool isConfirmed = await DisplayAlert("Confirm", "Are you sure to feed now?", "Yes", "Cancel");
        
        if (isConfirmed) 
        {
            String message = " ";
            if (Mode == ConnectionMode.WiFi)
                await MqttService.PublishMessageAsync(message, $"{Device.Id}/now");

            else if (Mode == ConnectionMode.Bluetooth)
                await BluetoothService.SendMessageAsync(Device.Id, message, "12345678-1234-5678-1234-56789abcdef4");
        }
    }

    private async void OnResetArduinoClicked(object sender, EventArgs e)
    {
        bool isConfirmed = await DisplayAlert("Confirm", "Are you sure to reset the Arduino? This will only turn the device off and on, and will NOT affect its current settings. You will be disconnected from the device momentarily", "Yes", "Cancel");

        if (isConfirmed)
        {
            String message = " ";
            if (Mode == ConnectionMode.WiFi)
                await MqttService.PublishMessageAsync(message, $"{Device.Id}/reset");

            else if (Mode == ConnectionMode.Bluetooth)
                await BluetoothService.SendMessageAsync(Device.Id, message, "12345678-1234-5678-1234-56789abcdef6");

            await DisplayAlert("Success", "Resetting Arduino. Expect disconnection in a moment.", "OK");
        }
    }

    private void OnNewProfileClicked(object sender, EventArgs e)
    {
        if (Application.Current.MainPage is MainPage mainPage)
        {
            var arduino = new ArduinoDevice
            {
                Id = $"New Profile {simCount}",
                Status = "Online"
            };
            mainPage.Devices.Add(arduino);
            mainPage.Detail = new NavigationPage(new ArduinoTabbedPage(arduino, ConnectionMode.Simulation));
            simCount++;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Not in mainPage");
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
                if (Mode == ConnectionMode.Bluetooth)
                {
                    await BluetoothService.SendMessageAsync(Device.Id, ssid, "12345678-1234-5678-1234-56789abcdef7");
                    await BluetoothService.SendMessageAsync(Device.Id, pass, "12345678-1234-5678-1234-56789abcdef8");

                    await DisplayAlert("Success", "Data sent to the device", "OK");
                }

                
            }
        }
    }
    /*
    private async void OnSetPHClicked(object sender, EventArgs e)
    {
        var ph = await this.ShowPopupAsync<string>(new SetPHPopup());
        if (ph != null)
        {
            if (Mode == ConnectionMode.WiFi)
            {
                await MqttService.PublishMessageAsync("{\"id\":\"" + "arduino-1" + "\",\"pH\":\"" + $"{ph.Result}" + "\"}", "sensors/pH");

                try
                {
                    var client = new HttpClient();
                    string url = $"{CONFIDENTIAL.DATABASE_URL}/feeders/{Device.Id}.json?auth={CONFIDENTIAL.DATABASE_SECRET}";

                    var response = await client.GetAsync(url);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    var newToken = new Dictionary<string, int>
                    {
                        { "ph", int.Parse(ph.Result) }
                    };
                    string json = JsonSerializer.Serialize(newToken);
                    await client.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to get token: {ex.Message}");
                }
            }
        }
    }*/

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        Preferences.Set("fishType_" + Device.Id, TypeEntry.Text);
        Preferences.Set("fishQuantity_" + Device.Id, QuantityEntry.Text);
        await DisplayAlert("Success", "Fish profile saved.", "OK");
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
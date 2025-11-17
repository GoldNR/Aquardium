using System.Text;
using System.Text.Json;
using MQTTnet;
using CommunityToolkit.Mvvm.Messaging;
using Plugin.Firebase.CloudMessaging;

namespace Aquardium;

public class MqttService
{
    private static IMqttClient mqttClient;
    private MqttClientOptions mqttOptions;
    private Label statusLabel;
    private Button reconnectButton;
    private Border devicesBorder;
    private CollectionView devicesList;
    private MainPage mainPage;

    public MqttService(Label statusLabel, Button reconnectButton, Border devicesBorder, CollectionView devicesList)
    {
        this.statusLabel = statusLabel;
        this.reconnectButton = reconnectButton;
        this.devicesBorder = devicesBorder;
        this.devicesList = devicesList;
        this.statusLabel.Text = "Connecting to Internet...";

        mqttClient = new MqttClientFactory().CreateMqttClient();
        mqttOptions = new MqttClientOptionsBuilder()
            .WithClientId("Aquardium")
            .WithTcpServer("broker.hivemq.com", 1883)
            .WithCleanSession()
            .Build();
        mqttClient.ApplicationMessageReceivedAsync += HandleReceivedApplicationMessage;

        mainPage = new MainPage { Mode = ConnectionMode.WiFi };
        devicesList.ItemsSource = mainPage.Devices;
    }

    public MainPage MainPage => mainPage;

    public async Task ConnectAsync()
    {
        try
        {
            if (!mqttClient.IsConnected)
            {
                var connectResult = await mqttClient.ConnectAsync(mqttOptions);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    statusLabel.Text = connectResult.ResultCode == MqttClientConnectResultCode.Success
                        ? "Connected to Internet. Waiting for Arduino..."
                        : $"Failed to connect: {connectResult.ResultCode}";
                });

                if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    await mqttClient.SubscribeAsync("status");
                    await mqttClient.SubscribeAsync("sensors/temperature");
                    await mqttClient.SubscribeAsync("sensors/turbidity");
                    await mqttClient.SubscribeAsync("sensors/pH");
                    await mqttClient.SubscribeAsync("sensors/timeLastFed");
                }
            }
        }
        catch (Exception)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                statusLabel.Text = "No Internet connection!";
                reconnectButton.IsEnabled = true;
                reconnectButton.IsVisible = true;
                devicesBorder.IsVisible = false;
            });
        }
    }

    private Task HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var payloadString = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(payloadString);
                var arduinoId = payload.GetValueOrDefault("id", "unknown");
        
                switch (e.ApplicationMessage.Topic)
                {
                    case "status":
                        var status = payload.GetValueOrDefault("status", "unknown");

                        if (status == "online")
                            HandleArduinoOnline(arduinoId);

                        else if (status == "offline")
                            HandleArduinoOffline(arduinoId);
                        break;

                    case "sensors/temperature":
                        var temperature = payload.GetValueOrDefault("temp", "unknown");
                        WeakReferenceMessenger.Default.Send(new TemperatureUpdateMessage(arduinoId, temperature));
                        break;

                    case "sensors/turbidity":
                        var turbidity = payload.GetValueOrDefault("turbidity", "unknown");
                        WeakReferenceMessenger.Default.Send(new TurbidityUpdateMessage(arduinoId, turbidity));
                        break;

                    case "sensors/pH":
                        var pH = payload.GetValueOrDefault("pH", "unknown");
                        WeakReferenceMessenger.Default.Send(new pHUpdateMessage(arduinoId, pH));
                        break;

                    case "sensors/timeLastFed":
                        var tlf = payload.GetValueOrDefault("timeLastFed", "unknown");
                        WeakReferenceMessenger.Default.Send(new TimeLastFedUpdateMessage(arduinoId, tlf));
                        break;
                }
            }

            catch (JsonException)
            {
                Console.WriteLine("Received invalid JSON payload.");
                return;
            }
        });

        return Task.CompletedTask;
    }

    private void HandleArduinoOnline(string arduinoId)
    {
        if (Application.Current.MainPage is MainPage)
        {
            if (mainPage.Devices == null)
                mainPage.Devices = new System.Collections.ObjectModel.ObservableCollection<ArduinoDevice>();

            var device = mainPage.Devices.FirstOrDefault(d => d.Id == arduinoId);

            if (device == null) // If Arduino is not in the list, add it
            {
                device = new ArduinoDevice { Id = arduinoId, Status = "Online" };
                RegisterDeviceTokenAsync(arduinoId);
                mainPage.Devices.Add(device);
                mainPage.Detail = new NavigationPage(new ArduinoTabbedPage(device, ConnectionMode.WiFi));
            }
            else // If Arduino is in the list, update its status
            {
                device.Status = "Online";
                WeakReferenceMessenger.Default.Send(new StatusUpdateMessage(arduinoId, "Online"));
            }
        }

        else if (Application.Current.MainPage is NavigationPage navPage &&
            navPage.CurrentPage is ConnectionPage)
        {
            if (mainPage.Devices == null)
                mainPage.Devices = new System.Collections.ObjectModel.ObservableCollection<ArduinoDevice>();

            var device = mainPage.Devices.FirstOrDefault(d => d.Id == arduinoId);
            if (device == null) 
            {
                device = new ArduinoDevice { Id = arduinoId, Status = "Online" };
                RegisterDeviceTokenAsync(arduinoId);

                mainPage.Devices.Add(device);
                if (mainPage.Devices.Count > 0)
                {
                    devicesBorder.IsVisible = true;
                    statusLabel.Text = "Choose an Arduino to view stats and controls of.";
                }
            }

            foreach (var arduino in mainPage.Devices)
            {
                arduino.Type = Preferences.Get($"fishType_{arduino.Id}", "Unknown");
                arduino.Quantity = Preferences.Get($"fishQuantity_{arduino.Id}", "Unknown");
            }
        }
    }

    private void HandleArduinoOffline(string arduinoId)
    {
        if (Application.Current?.MainPage is MainPage)
        {
            var device = mainPage.Devices.FirstOrDefault(d => d.Id == arduinoId);

            if (device != null)
            {
                device.Status = "Offline";
                WeakReferenceMessenger.Default.Send(new StatusUpdateMessage(device.Id, "Offline"));
            }

            if (mainPage.Devices.All(d => d.Status == "Offline"))
                ShowReconnectionAlert();
        }
    }

    private async void ShowReconnectionAlert()
    {
        bool reconnected = false;

        while (!reconnected)
        {
            await (Application.Current.MainPage).DisplayAlert(
                "Connection Lost",
                "No Arduinos connected. Reconnecting...",
                "OK"
            );

            if (Application.Current.MainPage is MainPage && mainPage.Devices.Any(s => s.Status == "Online"))
                reconnected = true;
        }
    }

    public static async Task PublishMessageAsync(string message, string topic)
    {
        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(message)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await mqttClient.PublishAsync(mqttMessage);
    }

    private async Task RegisterDeviceTokenAsync(string deviceId)
    {
        try
        {
            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            Console.WriteLine($"Device Token: {token}");

            var client = new HttpClient();
            string url = $"{CONFIDENTIAL.DATABASE_URL}/feeders/{deviceId}/tokens.json?auth={CONFIDENTIAL.DATABASE_SECRET}";

            var response = await client.GetAsync(url);
            string responseBody = await response.Content.ReadAsStringAsync();

            var existingTokens = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);

            bool tokenExists = existingTokens?.Values.Contains(token) ?? false;

            if (!tokenExists)
            {
                string uniqueKey = Guid.NewGuid().ToString();
                var newToken = new Dictionary<string, string>
                {
                    { uniqueKey, token }
                };

                string json = JsonSerializer.Serialize(newToken);
                await client.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get token: {ex.Message}");
        }
    }
}
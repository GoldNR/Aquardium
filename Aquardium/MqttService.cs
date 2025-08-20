using System.Text;
using System.Text.Json;
using MQTTnet;
using CommunityToolkit.Mvvm.Messaging;

namespace Aquardium;

public class MqttService
{
    private static IMqttClient mqttClient;
    private MqttClientOptions mqttOptions;
    private Label statusLabel;
    private Button reconnectButton;

    public MqttService(Label statusLabel, Button reconnectButton)
    {
        this.statusLabel = statusLabel;
        this.reconnectButton = reconnectButton;
        this.statusLabel.Text = "Connecting to Internet...";
        mqttClient = new MqttClientFactory().CreateMqttClient();
        mqttOptions = new MqttClientOptionsBuilder()
            .WithClientId("Aquardium")
            .WithTcpServer("broker.hivemq.com", 1883)
            .WithCleanSession()
            .Build();
        mqttClient.ApplicationMessageReceivedAsync += HandleReceivedApplicationMessage;
    }

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

                if (e.ApplicationMessage.Topic == "status")
                {
                    var status = payload.GetValueOrDefault("status", "unknown");

                    if (status == "online")
                        HandleArduinoOnline(arduinoId);

                    else if (status == "offline")
                        HandleArduinoOffline(arduinoId);
                }

                else if (e.ApplicationMessage.Topic == "sensors/temperature")
                {
                    var temperature = payload.GetValueOrDefault("temp", "unknown");
                    WeakReferenceMessenger.Default.Send(new TemperatureUpdateMessage(arduinoId, temperature));
                }

                else if (e.ApplicationMessage.Topic == "sensors/turbidity")
                {
                    var turbidity = payload.GetValueOrDefault("turbidity", "unknown");
                    WeakReferenceMessenger.Default.Send(new TurbidityUpdateMessage(arduinoId, turbidity));
                }

                else if (e.ApplicationMessage.Topic == "sensors/timeLastFed")
                {
                    var tlf = payload.GetValueOrDefault("timeLastFed", "unknown");
                    WeakReferenceMessenger.Default.Send(new TimeLastFedUpdateMessage(arduinoId, tlf));
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
        statusLabel.Text = "Arduino connected! Please wait...";

        if (Application.Current.MainPage is MainPage mainPage)
        {
            if (mainPage.Devices == null)
                mainPage.Devices = new System.Collections.ObjectModel.ObservableCollection<ArduinoDevice>();

            var device = mainPage.Devices.FirstOrDefault(d => d.Id == arduinoId);

            if (device == null) // If Arduino is not in the list, add it
            {
                device = new ArduinoDevice { Id = arduinoId, Status = "Online" };
                mainPage.Devices.Add(device);
                mainPage.Detail = new NavigationPage(new ArduinoTabbedPage(device, "WIFI"));
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
            var device = new ArduinoDevice
            {
                Id = arduinoId,
                Status = "Online"
            };

            var newMainPage = new MainPage
            {
                connectionMode = "WIFI",
                Detail = new NavigationPage(new ArduinoTabbedPage(device, "WIFI"))
            };

            newMainPage.Devices.Add(device);

            Application.Current.MainPage = newMainPage;
        }
    }

    private void HandleArduinoOffline(string arduinoId)
    {
        if (Application.Current?.MainPage is MainPage mainPage)
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

            if (Application.Current.MainPage is MainPage mainPage && mainPage.Devices.Any(s => s.Status == "Online"))
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
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using CommunityToolkit.Mvvm.Messaging;

namespace Aquardium
{
    public class MqttService
    {
        private readonly IMqttClient mqttClient;
        private MqttClientOptions mqttOptions;
        private readonly Label statusLabel;
        private readonly Button reconnectButton;

        public MqttService(Label statusLabel, Button reconnectButton)
        {
            this.statusLabel = statusLabel;
            this.reconnectButton = reconnectButton;
            mqttClient = new MqttFactory().CreateMqttClient();
            mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("Aquardium")
                .WithTcpServer("broker.hivemq.com", 1883)
                .WithCleanSession()
                .Build();

            ConnectAsync();

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
                            ? "Connected to MQTT broker. Waiting for Arduino..."
                            : $"Failed to connect: {connectResult.ResultCode}";
                    });

                    if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                    {
                        await mqttClient.SubscribeAsync("status");
                    }
                }
            }
            catch (Exception)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    statusLabel.Text = "No internet connection!";
                    reconnectButton.IsEnabled = true;
                });
            }
        }

        private Task HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic == "status")
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment));
                    var arduinoId = payload.GetValueOrDefault("id", "unknown");
                    var status = payload.GetValueOrDefault("status", "unknown");

                    if (status == "online")
                    {
                        HandleArduinoOnline(arduinoId);
                    }
                    else if (status == "offline")
                    {
                        HandleArduinoOffline(arduinoId);
                    }
                });
            }

            else if (e.ApplicationMessage.Topic == "sensors/temperature")
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment));
                    var arduinoId = payload.GetValueOrDefault("id", "unknown");
                    var temperature = payload.GetValueOrDefault("temp", "unknown");

                    WeakReferenceMessenger.Default.Send(new TemperatureUpdateMessage(arduinoId, temperature));
                });
            }

            return Task.CompletedTask;
        }

        private async void HandleArduinoOnline(string arduinoId)
        {
            statusLabel.Text = "Arduino connected! Please wait...";

            if (Application.Current.MainPage is MainPage mainPage)
            {
                if (mainPage.Devices == null)
                    mainPage.Devices = new System.Collections.ObjectModel.ObservableCollection<ArduinoDevice>();

                var device = mainPage.Devices.FirstOrDefault(d => d.Id == arduinoId);

                if (device == null) // If Arduino is not in the list, add it
                {
                    await mqttClient.SubscribeAsync("sensors/temperature");     // Subscribe to sensors, add more if there's more sensors

                    device = new ArduinoDevice { Id = arduinoId, Status = "online" };
                    mainPage.Devices.Add(device);
                    mainPage.Detail = new NavigationPage(new ArduinoTabbedPage(device));
                }
                else // If Arduino is in the list, update its status
                {
                    device.Status = "online";
                }
            }

            else if (Application.Current.MainPage is NavigationPage navPage &&
             navPage.CurrentPage is ConnectionPage)
            {
                var newMainPage = new MainPage
                {
                    Detail = new NavigationPage(new ArduinoTabbedPage(new ArduinoDevice
                    {
                        Id = arduinoId,
                        Status = "online"
                    }))
                };

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
                    device.Status = "offline";
                }

                if (mainPage.Devices.All(d => d.Status == "offline"))
                {
                    ShowReconnectionAlert();
                }
            }
        }

        private async void ShowReconnectionAlert()
        {
            bool reconnected = false;

            // Loop until an Arduino reconnects
            while (!reconnected)
            {
                // Show the alert and wait for user acknowledgment
                await (Application.Current.MainPage).DisplayAlert(
                    "Connection Lost",
                    "No Arduinos connected. Reconnecting...",
                    "OK"
                );

                // Check if at least one Arduino has reconnected
                if (Application.Current.MainPage is MainPage mainPage && mainPage.Devices.Any(s => s.Status == "online"))
                {
                    reconnected = true;
                }
            }
        }

        public async Task PublishMessageAsync(string message, string topic)
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await mqttClient.PublishAsync(mqttMessage);
        }
    }
}

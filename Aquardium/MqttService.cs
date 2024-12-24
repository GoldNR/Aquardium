using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace Aquardium
{
    public class MqttService
    {
        private readonly IMqttClient mqttClient;
        private MqttClientOptions mqttOptions;
        private readonly Label statusLabel;
        private readonly Button reconnectButton;

        public MqttService(Button reconnectButton, Label statusLabel)
        {
            this.reconnectButton = reconnectButton;
            this.statusLabel = statusLabel;
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();
        }

        public async Task ConnectMqttAsync()
        {
            mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("MauiAppClient")
                .WithTcpServer("broker.hivemq.com", 1883)  // Public test broker
                .WithCleanSession()
                .Build();

            mqttClient.ApplicationMessageReceivedAsync += HandleReceivedApplicationMessage;

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
                        await mqttClient.SubscribeAsync("sensors/temperature");
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
            var message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            if (e.ApplicationMessage.Topic == "status")
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (AppShell.CurrentDashboardPage != null)
                        AppShell.CurrentDashboardPage.UpdateStatus(message); // TEST

                    var payloadJson = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
                    var arduinoId = payloadJson.GetValueOrDefault("id", "unknown");
                    var arduinoName = payloadJson.GetValueOrDefault("device", "arduino");
                    var status = payloadJson.GetValueOrDefault("status", "");

                    if (status == "online")
                    {
                        HandleArduinoOnline(arduinoId, arduinoName);
                    }
                    else if (status == "offline" && AppShell.CurrentDashboardPage != null)
                    {
                        HandleArduinoOffline(arduinoId);
                    }
                });
            }

            else if (e.ApplicationMessage.Topic == "sensors/temperature" && AppShell.CurrentDashboardPage != null)
                MainThread.InvokeOnMainThreadAsync(() => { AppShell.CurrentDashboardPage.UpdateTemperature(message); });

            return Task.CompletedTask;
        }

        private void HandleArduinoOnline(string arduinoId, string arduinoName)
        {
            statusLabel.Text = "Arduino Connected! Please wait...";
            var existingArduino = App.ArduinoList.FirstOrDefault(a => a.Id == arduinoId);

            if (existingArduino == null)
            {
                var newArduino = new ArduinoDevice { Name = arduinoName, Id = arduinoId };
                App.ArduinoList.Add(newArduino);

                (Shell.Current as AppShell).RegisterArduino(newArduino);
            }


            if (Shell.Current?.CurrentPage is MainPage)
            {
                // Reuse existing DashboardPage instance or create if null
                if (AppShell.CurrentDashboardPage == null)
                    AppShell.CurrentDashboardPage = new DashboardPage { SelectedArduino = existingArduino };

                Application.Current.MainPage = new NavigationPage(AppShell.CurrentDashboardPage);
            }
        }

        private void HandleArduinoOffline(string arduinoId)
        {
            //TEST
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
            AppShell.CurrentDashboardPage.DisplayAlert("ArduinoList Before", allArduinos);
            AppShell.CurrentDashboardPage.DisplayAlert("ShellItems Before", allShellItems);
            //TEST

            var arduinoToRemove = App.ArduinoList.FirstOrDefault(a => a.Id == arduinoId);
            if (arduinoToRemove != null)
            {
                App.ArduinoList.Remove(arduinoToRemove);
                var flyoutItem = Shell.Current?.Items.FirstOrDefault(x => x.Title == arduinoToRemove.Name);
                if (flyoutItem != null)
                {
                    Shell.Current?.Items.Remove(flyoutItem);
                    ShowReconnectionAlert(); // Remove this after test

                }
                //if (App.ArduinoList.Count == 0 && AppShell.CurrentDashboardPage != null)
                    ///ShowReconnectionAlert();

            }

            //TEST
            allArduinos = "";
            allShellItems = "";
            foreach (var ard in App.ArduinoList)
            {
                allArduinos += ard.Name + ", ";
            }
            if (Shell.Current != null)

                foreach (var sh in Shell.Current.Items)
            {
                allArduinos += sh.Title + ", ";
            }
            AppShell.CurrentDashboardPage.DisplayAlert("ArduinoList After", allArduinos);
            AppShell.CurrentDashboardPage.DisplayAlert("ShellItems After", allShellItems);
            //TEST
        }

        private async void ShowReconnectionAlert()
        {
            bool reconnected = false;

            // Loop until an Arduino reconnects
            while (!reconnected)
            {
                // Show the alert and wait for user acknowledgment
                await Application.Current.MainPage.DisplayAlert(
                    "Connection Lost",
                    "No Arduinos connected. Reconnecting...",
                    "OK"
                );

                // Check if at least one Arduino has reconnected
                if (App.ArduinoList.Count > 0)
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

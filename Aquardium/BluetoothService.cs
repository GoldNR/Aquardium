using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;

namespace Aquardium;

public class BluetoothService
{
    private static IAdapter _adapter;
    private readonly Label statusLabel;
    private readonly Button reconnectButton;

    public BluetoothService(Label statusLabel, Button reconnectButton)
    {
        this.statusLabel = statusLabel;
        this.reconnectButton = reconnectButton;
        this.statusLabel.Text = "Connecting via Bluetooth...";
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceConnected += HandleArduinoConnected;
        _adapter.DeviceConnectionLost += HandleArduinoDisconnected;

        Connect();
    }

    public void Connect()
    {
        var knownDevices = _adapter.GetSystemConnectedOrPairedDevices();
        foreach (var device in knownDevices)
        {
            if (device.Name.Contains("arduino"))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await _adapter.ConnectToDeviceAsync(device);
                        statusLabel.Text = $"Connected to {device.Name}! Please wait...";
                    }
                    catch (DeviceConnectionException dce)
                    {
                        statusLabel.Text = $"Failed to connect: {dce.Message}";
                        reconnectButton.IsEnabled = true;
                    }
                });
            }
        }
    }

    private async void HandleArduinoConnected(object? sender, DeviceEventArgs e)
    {
        await SubscribeToDataAsync(e.Device);

        MainThread.BeginInvokeOnMainThread (() =>
        {
            if (Application.Current.MainPage is MainPage mainPage)
            {
                if (mainPage.Devices == null)
                    mainPage.Devices = new System.Collections.ObjectModel.ObservableCollection<ArduinoDevice>();

                var device = mainPage.Devices.FirstOrDefault(d => d.Id == e.Device.Name);

                if (device == null) // If Arduino is not in the list, add it
                {
                    device = new ArduinoDevice { Id = e.Device.Name, Status = "Connected" };
                    mainPage.Devices.Add(device);
                    mainPage.Detail = new NavigationPage(new ArduinoTabbedPage(device, "BLUETOOTH", ref e.Device));
                }
                else // If Arduino is in the list, update its status
                {
                    device.Status = "Connected";
                    WeakReferenceMessenger.Default.Send(new StatusUpdateMessage(e.Device.Name, "Connected"));
                }
            }

            else if (Application.Current.MainPage is NavigationPage navPage &&
                navPage.CurrentPage is ConnectionPage)
            {
                var device = new ArduinoDevice
                {
                    Id = e.Device.Name,
                    Status = "Connected"
                };

                var newMainPage = new MainPage
                {
                    connectionMode = "BLUETOOTH",
                    Detail = new NavigationPage(new ArduinoTabbedPage(device, "BLUETOOTH", ref e.Device))
                };

                newMainPage.Devices.Add(device);

                Application.Current.MainPage = newMainPage;
            }
        });
    }

    private void HandleArduinoDisconnected(object? sender, DeviceEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => {
            if (Application.Current.MainPage is MainPage mainPage)
            {
                var device = mainPage.Devices.FirstOrDefault(d => d.Id == e.Device.Name);

                if (device != null)
                {
                    device.Status = "Disconnected";
                    WeakReferenceMessenger.Default.Send(new StatusUpdateMessage(device.Id, "Disconnected"));
                }

                if (mainPage.Devices.All(d => d.Status == "Disconnected"))
                    ShowReconnectionAlert();
            }
        });
    }

    private async void ShowReconnectionAlert()
    {
        bool reconnected = false;

        // Loop until an Arduino reconnects
        while (!reconnected)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Connect();
                await (Application.Current.MainPage).DisplayAlert(
                    "Connection Lost",
                    "No Arduinos connected. Reconnecting...",
                    "OK"
                );
            });

            if (Application.Current.MainPage is MainPage mainPage && mainPage.Devices.Any(s => s.Status == "Connected"))
                reconnected = true;
        }
    }

    private async Task SubscribeToDataAsync(IDevice device)
    {
        var services = await device.GetServicesAsync();
        Console.WriteLine($"Found {services.Count} services for device {device.Name}.");

        foreach (var service in services)
        {
            Console.WriteLine($"Service: {service.Id}");
            var tempCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("00002a6e-0000-1000-8000-00805f9b34fb"));

            if (tempCharacteristic == null)
            {
                Console.WriteLine("Failed to find temperature characteristic.");
                continue;
            }

            tempCharacteristic.ValueUpdated += (o, args) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeakReferenceMessenger.Default.Send(new TemperatureUpdateMessage(device.Name, Encoding.UTF8.GetString(args.Characteristic.Value)));
                });
            };

            await tempCharacteristic.StartUpdatesAsync();
            Console.WriteLine("Started updates for temperature characteristic.");
        }
    }

    public static async Task SendMessageAsync(string message, IDevice device, string characteristicGuid)
    {
        var services = await device.GetServicesAsync();

        Console.WriteLine($"Sending BT data: Found {services.Count} services for device {device.Name}.");

        foreach (var service in services)
        {
            Console.WriteLine($"Service: {service.Id}");
            var characteristics = await service.GetCharacteristicsAsync();
            Console.WriteLine($"Sending BT data: Found {characteristics.Count} characteristics for service {service.Id}.");
            foreach (var characteristic in characteristics)
            {
                Console.WriteLine($"Characteristic: {characteristic.Id}");
            }

            var characteristicToWrite = characteristics.FirstOrDefault(c => c.Id == Guid.Parse(characteristicGuid));
            if (characteristicToWrite == null)
            {
                Console.WriteLine("Characteristic to write not found!");
                await Application.Current.MainPage.DisplayAlert("Error", "Characteristic to write not found!", "OK");
                continue; //CONTINUE WORKING ON THIS
            }

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await characteristicToWrite.WriteAsync(messageBytes);
                    Console.WriteLine($"Sent message: {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to characteristic: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Error", $"Error writing to characteristic: {ex.Message}", "OK");
                }
            });
        }
    }
}
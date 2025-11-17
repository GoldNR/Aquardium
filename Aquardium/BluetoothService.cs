using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;

namespace Aquardium;

public class BluetoothService
{
    private static IAdapter _adapter;
    private Label statusLabel;
    private Button reconnectButton;
    private Border devicesBorder;
    private CollectionView devicesList;
    private static Dictionary<string, IDevice> connectedDevices = new();
    private MainPage mainPage;

    public BluetoothService(Label statusLabel, Button reconnectButton, Border devicesBorder, CollectionView devicesList)
    {
        this.statusLabel = statusLabel;
        this.reconnectButton = reconnectButton;
        this.devicesBorder = devicesBorder;
        this.devicesList = devicesList;
        this.statusLabel.Text = "Connecting via Bluetooth...";
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceConnected += HandleArduinoConnected;
        _adapter.DeviceConnectionLost += HandleArduinoDisconnected;

        mainPage = new MainPage { Mode = ConnectionMode.Bluetooth };
        devicesList.ItemsSource = mainPage.Devices;
    }

    public MainPage MainPage => mainPage;

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
                        statusLabel.Text = "Found device(s)! Please wait...";
                    }
                    catch (DeviceConnectionException dce)
                    {
                        statusLabel.Text = $"Failed to connect: {dce.Message}";
                        reconnectButton.IsEnabled = true;
                        reconnectButton.IsVisible = true;
                        devicesList.IsVisible = false;
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
            if (Application.Current.MainPage is MainPage)
            {
                if (mainPage.Devices == null)
                    mainPage.Devices = new System.Collections.ObjectModel.ObservableCollection<ArduinoDevice>();

                var device = mainPage.Devices.FirstOrDefault(d => d.Id == e.Device.Name);

                if (device == null) // If Arduino is not in the list, add it
                {
                    device = new ArduinoDevice { Id = e.Device.Name, Status = "Connected" };
                    mainPage.Devices.Add(device);
                    connectedDevices.Add(e.Device.Name, e.Device);
                    mainPage.Detail = new NavigationPage(new ArduinoTabbedPage(device, ConnectionMode.Bluetooth));
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
                if (mainPage.Devices == null)
                    mainPage.Devices = new System.Collections.ObjectModel.ObservableCollection<ArduinoDevice>();

                var device = mainPage.Devices.FirstOrDefault(d => d.Id == e.Device.Name);
                if (device == null)
                {
                    device = new ArduinoDevice { Id = e.Device.Name, Status = "Connected" };

                    connectedDevices.Add(e.Device.Name, e.Device);
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

            var characteristics = await service.GetCharacteristicsAsync();
            var tempCharacteristic = characteristics
                .FirstOrDefault(c => c.Id == Guid.Parse("12345678-1234-5678-1234-56789abcdef1"));
            var turbidityCharacteristic = characteristics
                .FirstOrDefault(c => c.Id == Guid.Parse("12345678-1234-5678-1234-56789abcdef2"));
            var pHCharacteristic = characteristics
                .FirstOrDefault(c => c.Id == Guid.Parse("12345678-1234-5678-1234-56789abcdef9"));
            var timeLastFedCharacteristic = characteristics
                .FirstOrDefault(c => c.Id == Guid.Parse("12345678-1234-5678-1234-56789abcdef5"));
            // Add new characteristic here when applicable

            if (tempCharacteristic == null ||
                turbidityCharacteristic == null ||
                timeLastFedCharacteristic == null ||
                pHCharacteristic == null)
            {
                Console.WriteLine("Failed to find characteristics.");
                continue;
            }

            tempCharacteristic.ValueUpdated += (o, args) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeakReferenceMessenger.Default.Send(new TemperatureUpdateMessage(device.Name, Encoding.UTF8.GetString(args.Characteristic.Value)));
                });
            };

            turbidityCharacteristic.ValueUpdated += (o, args) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeakReferenceMessenger.Default.Send(new TurbidityUpdateMessage(device.Name, Encoding.UTF8.GetString(args.Characteristic.Value)));
                });
            };

            pHCharacteristic.ValueUpdated += (o, args) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeakReferenceMessenger.Default.Send(new pHUpdateMessage(device.Name, Encoding.UTF8.GetString(args.Characteristic.Value)));
                });
            };

            timeLastFedCharacteristic.ValueUpdated += (o, args) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeakReferenceMessenger.Default.Send(new TimeLastFedUpdateMessage(device.Name, Encoding.UTF8.GetString(args.Characteristic.Value)));
                });
            };
            // Add new characteristic here when applicable

            await tempCharacteristic.StartUpdatesAsync();
            await turbidityCharacteristic.StartUpdatesAsync();
            await pHCharacteristic.StartUpdatesAsync();
            await timeLastFedCharacteristic.StartUpdatesAsync();
            // Add new characteristic here when applicable
        }
    }

    public static async Task SendMessageAsync(string deviceId, string message, string characteristicGuid)
    {
        var services = await connectedDevices[deviceId].GetServicesAsync();

        Console.WriteLine($"Sending BT data: Found {services.Count} services for device {deviceId}.");

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
                continue;
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
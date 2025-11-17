using Microsoft.Maui.Networking;
using Microsoft.Maui.Controls;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace Aquardium;

public partial class ConnectionPage : ContentPage
{
	private MqttService mqttService;
    private BluetoothService bluetoothService;
    private MainPage mainPage;
    public ConnectionPage()
	{
		InitializeComponent();
        CheckConnection();
	}

	private async void CheckConnection()
	{
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            mqttService = new MqttService(StatusLabel, ReconnectButton, DevicesBorder, DevicesList);
            await mqttService.ConnectAsync();
            mainPage = mqttService.MainPage;
        }

        else if (CrossBluetoothLE.Current.IsOn)
        {
            bluetoothService = new BluetoothService(StatusLabel, ReconnectButton, DevicesBorder, DevicesList);
            bluetoothService.Connect();
            mainPage = bluetoothService.MainPage;
        }

        else
        {
            StatusLabel.Text = "Not connected to Wi-Fi nor Bluetooth. Please turn on one of them.";
            ReconnectButton.IsEnabled = true;
            ReconnectButton.IsVisible = true;
        }
    }

	private void OnReconnectClicked(object sender, EventArgs e)
    {
        ReconnectButton.IsEnabled = false;
        ReconnectButton.IsVisible = false;
        CheckConnection();
    }

    private void DevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ArduinoDevice selectedDevice)
        {
            mainPage.Detail = new NavigationPage(new ArduinoTabbedPage(selectedDevice, ConnectionMode.WiFi));
            Application.Current.MainPage = mainPage;
        }
        ((CollectionView)sender).SelectedItem = null;
    }
}
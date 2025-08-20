using Microsoft.Maui.Networking;
using Microsoft.Maui.Controls;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace Aquardium;

public partial class ConnectionPage : ContentPage
{
	private MqttService mqttService;
    private BluetoothService bluetoothService;
    public ConnectionPage()
	{
		InitializeComponent();
        CheckConnection();
	}

	private async void CheckConnection()
	{
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            mqttService = new MqttService(StatusLabel, ReconnectButton);
            await mqttService.ConnectAsync();
        }

        else if (CrossBluetoothLE.Current.IsOn)
        {
            bluetoothService = new BluetoothService(StatusLabel, ReconnectButton);
            bluetoothService.Connect();
        }

        else
        {
            StatusLabel.Text = "Not connected to Wi-Fi nor Bluetooth. Please turn on one of them.";
            ReconnectButton.IsEnabled = true;
        }
    }

	private void OnReconnectClicked(object sender, EventArgs e)
    {
        ReconnectButton.IsEnabled = false;
        CheckConnection();
    }
}
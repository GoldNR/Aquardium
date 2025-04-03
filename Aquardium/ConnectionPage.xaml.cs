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

	private void CheckConnection()
	{
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet &&
           (Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.WiFi) ||
            Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.Ethernet)))
            mqttService = new MqttService(StatusLabel, ReconnectButton);

        else if (CrossBluetoothLE.Current.IsOn)
            bluetoothService = new BluetoothService(StatusLabel, ReconnectButton);

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
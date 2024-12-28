using MQTTnet;

namespace Aquardium;

public partial class ConnectionPage : ContentPage
{
	private MqttService mqttService;
	public ConnectionPage()
	{
		InitializeComponent();
		mqttService = new MqttService(StatusLabel, ReconnectButton);
	}

	private async void OnReconnectClicked(object sender, EventArgs e)
    {
        await mqttService.ConnectAsync();
    }
}
using MQTTnet;
using System.Collections.ObjectModel;
using System.Text;

namespace Aquardium
{
    public partial class MainPage : ContentPage
    {
        private MqttService mqttService;

        public MainPage()
        {
            InitializeComponent();
            if (App.ArduinoList == null)
            {
                App.ArduinoList = new ObservableCollection<ArduinoDevice>();
            }
            mqttService = new MqttService(ReconnectButton, StatusLabel);
            ConnectToMqtt();
            
        }

        public void DisplayAlert(string title, string message)
        {
            DisplayAlert(title, message, "OK");
        }

        private async void ConnectToMqtt()
        {
            await mqttService.ConnectMqttAsync();
        }

        private void OnReconnectClicked(object sender, EventArgs e)
        {
            ConnectToMqtt();
            ReconnectButton.IsEnabled = false;
        }
    }

}

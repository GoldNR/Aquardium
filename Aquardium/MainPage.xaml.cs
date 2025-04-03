using System.Collections.ObjectModel;

namespace Aquardium;

public partial class MainPage : FlyoutPage
{
    public ObservableCollection<ArduinoDevice> Devices { get; set; }
    public string connectionMode;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        Devices = new ObservableCollection<ArduinoDevice>();
        DeviceListView.ItemsSource = Devices;
    }

    private void OnDeviceSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is ArduinoDevice selectedDevice)
        {
            DeviceListView.SelectedItem = null;
            Detail = new NavigationPage(new ArduinoTabbedPage(selectedDevice, connectionMode));
        }
    }
}

public class ArduinoDevice
{
    public string Id { get; set; }
    public string Status { get; set; }
}
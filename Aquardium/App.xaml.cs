namespace Aquardium
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new ConnectionPage());
            //MainPage = new NavigationPage(new MainPage() { Detail = new NavigationPage(new ArduinoTabbedPage(new ArduinoDevice { Id = "Simulated Arduino", Status = "online" })), Devices = new System.Collections.ObjectModel.ObservableCollection<ArduinoDevice> { new ArduinoDevice { Id = "Simulated Arduino", Status = "online" }}});
        }
    }
}

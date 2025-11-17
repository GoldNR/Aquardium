namespace Aquardium;

public partial class App : Application
{
    private bool simulate = false;
    public App()
    {
        InitializeComponent();
        if (!simulate)
            MainPage = new NavigationPage(new ConnectionPage());
        else
            SimulateArduino();
    }

    private void SimulateArduino()
    {
        var arduino = new ArduinoDevice
        {
            Id = "Simulated Arduino",
            Status = "Online"
        };

        var mainPage = new MainPage();
        mainPage.Devices.Add(arduino);

        mainPage.Detail = new NavigationPage(
            new ArduinoTabbedPage(arduino, ConnectionMode.Simulation)
        );

        MainPage = mainPage;
    }
}
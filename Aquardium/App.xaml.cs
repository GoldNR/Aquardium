using System.Collections.ObjectModel;

namespace Aquardium
{
    public partial class App : Application
    {
        public static ObservableCollection<ArduinoDevice> ArduinoList { get; set; }

        public App()
        {
            InitializeComponent();
            ArduinoList = new ObservableCollection<ArduinoDevice>();
            MainPage = new AppShell();
        }
    }
}

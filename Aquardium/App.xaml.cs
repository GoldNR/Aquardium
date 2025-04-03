using Firebase.Database;
using Firebase.Database.Query;
using Plugin.Firebase.CloudMessaging;

namespace Aquardium;

public partial class App : Application
{
    private const string FirebaseUrl = "https://aquardium-push-notif-default-rtdb.asia-southeast1.firebasedatabase.app";
    private const string NotificationPath = "notifications/low_feed";
    private FirebaseClient firebaseClient;

    public App()
    {
        InitializeComponent();
        firebaseClient = new FirebaseClient(FirebaseUrl);
        

        MainPage = new NavigationPage(new ConnectionPage());
        //MainPage = new NavigationPage(new MainPage() { Detail = new NavigationPage(new ArduinoTabbedPage(new ArduinoDevice { Id = "Simulated Arduino", Status = "online" })), Devices = new System.Collections.ObjectModel.ObservableCollection<ArduinoDevice> { new ArduinoDevice { Id = "Simulated Arduino", Status = "online" }}});
    }

    private async void SubscribeToNotifications()
    {
        await CrossFirebaseCloudMessaging.Current.SubscribeToTopicAsync("low_feed_alert");
    }
}
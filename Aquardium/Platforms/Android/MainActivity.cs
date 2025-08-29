using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Firebase;
using Plugin.Firebase.CloudMessaging;

namespace Aquardium
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                var app = FirebaseApp.InitializeApp(this);
                if (app == null)
                {
                    var options = new FirebaseOptions.Builder()
                        .SetApplicationId(CONFIDENTIAL.APP_ID)
                        .SetApiKey(CONFIDENTIAL.API_KEY)
                        .SetDatabaseUrl(CONFIDENTIAL.DATABASE_URL)
                        .SetGcmSenderId(CONFIDENTIAL.SENDER_ID)
                        .Build();

                    FirebaseApp.InitializeApp(this, options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FIREBASE INIT ERROR] {ex.Message}");
            }

            HandleIntent(Intent);
            CreateNotificationChannelIfNeeded();
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
        }

        private static void HandleIntent(Intent intent)
        {
            FirebaseCloudMessagingImplementation.OnNewIntent(intent);
        }

        private void CreateNotificationChannelIfNeeded()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                CreateNotificationChannel();
            }
        }

        private void CreateNotificationChannel()
        {
            var channelId = $"{PackageName}.general";
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            var channel = new NotificationChannel(channelId, "General", NotificationImportance.Default);
            notificationManager.CreateNotificationChannel(channel);
            FirebaseCloudMessagingImplementation.ChannelId = channelId;
        }
    }
}

using Foundation;
using UIKit;
using Firebase;
using UserNotifications;

namespace Aquardium
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            // ✅ Initialize Firebase for iOS
            Firebase.Core.App.Configure();

            // ✅ Request notification permissions
            UNUserNotificationCenter.Current.RequestAuthorization(
                UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
                (approved, err) =>
                {
                    if (approved)
                    {
                        Console.WriteLine("Push notification permission granted.");
                    }
                    else
                    {
                        Console.WriteLine($"Push notification permission denied: {err?.LocalizedDescription}");
                    }
                });

            // ✅ Register for remote notifications
            UIApplication.SharedApplication.RegisterForRemoteNotifications();

            return base.FinishedLaunching(app, options);
        }
    }
}

using Android.App;
using Android.Content;
using Android.Util;
using Firebase.Messaging;

namespace MedSestriManipulations.Platforms.Android.Services
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        const string TAG = "MyFirebaseMsgService";

        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            Log.Debug(TAG, "From: " + message.From);

            var data = message.Data;
            var title = message.GetNotification()?.Title ?? "Notification";
            var body = message.GetNotification()?.Body ?? "";

            SendNotification(title, body, data);
        }

        void SendNotification(string title, string body, IDictionary<string, string> data)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);

            foreach (var key in data.Keys)
            {
                intent.PutExtra(key, data[key]);
            }

            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);

            var notificationBuilder = new Notification.Builder(this, "default")
                .SetContentTitle(title)
                .SetContentText(body)
                .SetSmallIcon(Resource.Mipmap.medsestr_icon)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(this);

            notificationManager?.Notify(0, notificationBuilder.Build());
        }
    }
}

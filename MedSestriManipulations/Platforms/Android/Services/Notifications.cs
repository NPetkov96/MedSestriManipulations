namespace MedSestriManipulations.Platforms.Android.Services
{
    public class Notifications : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            new List<(string permission, bool)> {
                (global::Android.Manifest.Permission.PostNotifications, true)
            }.ToArray();
    }
}

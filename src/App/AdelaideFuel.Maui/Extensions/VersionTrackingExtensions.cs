namespace AdelaideFuel.Maui.Extensions
{
    internal static class VersionTrackingExtensions
    {
        public static void Migrate(this IVersionTracking _)
        {
            const string VersionTrailKey = "VersionTracking.Trail";
            const string VersionsKey = "VersionTracking.Versions";
            const string BuildsKey = "VersionTracking.Builds";

            const string XamarinSharedGroupFmt = "{0}.xamarinessentials.versiontracking";
            const string MauiSharedGroupFmt = "{0}.microsoft.maui.versiontracking";

            var xamGroupName = string.Format(XamarinSharedGroupFmt, AppInfo.PackageName);
            var mauiGroupName = string.Format(MauiSharedGroupFmt, AppInfo.PackageName);

            Migrate(VersionTrailKey, xamGroupName, mauiGroupName);
            Migrate(VersionsKey, xamGroupName, mauiGroupName);
            Migrate(BuildsKey, xamGroupName, mauiGroupName);
        }

        private static bool Migrate(string key, string oldSharedGroup, string newSharedGroup)
        {
            if (Preferences.ContainsKey(key, oldSharedGroup))
            {
                var data = Preferences.Get(key, null, oldSharedGroup);
                Preferences.Set(key, data, newSharedGroup);
                Preferences.Remove(key, oldSharedGroup);
                return true;
            }

            return false;
        }
    }
}
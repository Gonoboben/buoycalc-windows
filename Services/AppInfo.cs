namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.38.2";
    public const string VersionNote = "short line warning";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}

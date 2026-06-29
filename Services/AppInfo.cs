namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.46";
    public const string VersionNote = "release build preparation";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
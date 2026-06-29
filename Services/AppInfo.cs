namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.46.2";
    public const string VersionNote = "user and full reports";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
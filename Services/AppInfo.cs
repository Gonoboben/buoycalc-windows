namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.37.1";
    public const string VersionNote = "build CI";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}

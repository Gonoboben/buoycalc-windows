namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.46.1";
    public const string VersionNote = "PDF report cleanup";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
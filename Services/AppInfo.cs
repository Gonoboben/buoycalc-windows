namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.45.2";
    public const string VersionNote = "PDF discrete shape";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
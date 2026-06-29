namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.40";
    public const string VersionNote = "discrete loads primary solver gate";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
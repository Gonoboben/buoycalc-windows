namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.45.1";
    public const string VersionNote = "Russian PDF explanations";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
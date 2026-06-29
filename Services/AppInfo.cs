namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.35.1";
    public const string VersionNote = "alt shape warning";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}

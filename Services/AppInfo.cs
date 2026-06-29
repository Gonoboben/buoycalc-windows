namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.44";
    public const string VersionNote = "sequence editor UX";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
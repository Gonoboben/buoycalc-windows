namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.40.2";
    public const string VersionNote = "primary shape selection table";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
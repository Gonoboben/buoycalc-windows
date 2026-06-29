namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.39.2";
    public const string VersionNote = "iteration report diagnostics";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
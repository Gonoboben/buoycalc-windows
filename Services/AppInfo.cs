namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.43";
    public const string VersionNote = "element database autochecks";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
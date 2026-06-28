namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.33";
    public const string VersionNote = "sequence positions";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}

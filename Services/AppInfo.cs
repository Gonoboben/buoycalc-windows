namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.38.4";
    public const string VersionNote = "PDF solver diagram";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}

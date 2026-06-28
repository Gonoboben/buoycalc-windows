namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.30";
    public const string VersionNote = "shape projection bridge";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " · " + VersionNote;
}

namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.46.5";
    public const string VersionNote = "2D выбранная форма";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
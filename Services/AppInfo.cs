namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.36";
    public const string VersionNote = "alt XZ nodes";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}

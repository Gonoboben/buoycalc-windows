namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.46.3";
    public const string VersionNote = "полный отчёт и русские автопроверки";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
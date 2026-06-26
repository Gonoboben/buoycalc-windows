namespace BuoyCalc.Windows.Models;

public sealed class LibraryOption
{
    public LibraryOption(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }
    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}

using System;

namespace BuoyCalc.Windows.Services;

public static class UserStatusPolicy
{
    public static string ToUserVerdict(string value)
    {
        value = Clean(value);

        if (string.IsNullOrWhiteSpace(value))
        {
            return "Не определено";
        }

        return value;
    }

    public static string ToUserStatus(string value)
    {
        value = Clean(value);

        if (string.IsNullOrWhiteSpace(value))
        {
            return "не определено";
        }

        if (StartsWith(value, "OK"))
        {
            return RemoveTechnicalPrefix(value, "OK", "подходит");
        }

        if (StartsWith(value, "INFO"))
        {
            return RemoveTechnicalPrefix(value, "INFO", "информационное примечание");
        }

        if (StartsWith(value, "WARNING"))
        {
            return RemoveTechnicalPrefix(value, "WARNING", "требует проверки");
        }

        if (StartsWith(value, "FAILED") || StartsWith(value, "ERROR"))
        {
            return RemoveAnyTechnicalPrefix(value, "не подходит");
        }

        return value;
    }

    public static string ToUserRisk(string value)
    {
        value = ToUserStatus(value);

        if (string.IsNullOrWhiteSpace(value))
        {
            return "Критичных рисков не найдено";
        }

        return UpperFirst(value);
    }

    private static string RemoveAnyTechnicalPrefix(string value, string fallback)
    {
        if (StartsWith(value, "FAILED"))
        {
            return RemoveTechnicalPrefix(value, "FAILED", fallback);
        }

        if (StartsWith(value, "ERROR"))
        {
            return RemoveTechnicalPrefix(value, "ERROR", fallback);
        }

        return fallback;
    }

    private static string RemoveTechnicalPrefix(string value, string prefix, string fallback)
    {
        var text = value[prefix.Length..].TrimStart();
        if (text.StartsWith(":"))
        {
            text = text[1..].TrimStart();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        return fallback + ": " + text;
    }

    private static bool StartsWith(string value, string prefix)
    {
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string Clean(string value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static string UpperFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}

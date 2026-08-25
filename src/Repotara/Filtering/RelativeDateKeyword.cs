namespace Repotara.Filtering;

/// <summary>
/// String constants recognized as relative date values inside a
/// <c>SearchParam.Value</c> when comparing a date/time property. Each resolves
/// to a full calendar-day (or week/month) range, not a single instant.
/// </summary>
public static class RelativeDateKeyword
{
    /// <summary>The full current calendar day.</summary>
    public const string Today = "TODAY";

    /// <summary>The full previous calendar day.</summary>
    public const string Yesterday = "YESTERDAY";

    /// <summary>The current calendar week (Sunday through Saturday).</summary>
    public const string ThisWeek = "THIS_WEEK";

    /// <summary>The current calendar month.</summary>
    public const string ThisMonth = "THIS_MONTH";

    /// <summary>The previous calendar week.</summary>
    public const string LastWeek = "LAST_WEEK";

    /// <summary>The previous calendar month.</summary>
    public const string LastMonth = "LAST_MONTH";

    private static readonly HashSet<string> All =
    [
        Today, Yesterday, ThisWeek, ThisMonth, LastWeek, LastMonth
    ];

    /// <summary>Returns true if the given text is a recognized relative date keyword.</summary>
    public static bool IsKeyword(string? text)
    {
        return text != null && All.Contains(text);
    }
}

namespace CleanArchTemplate.Shared.Extensions;

/// <summary>
/// Extension methods for DateTime operations
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to Unix timestamp
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>The Unix timestamp</returns>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts a DateTime to Unix timestamp in milliseconds
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>The Unix timestamp in milliseconds</returns>
    public static long ToUnixTimestampMilliseconds(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Checks if a DateTime is in the past
    /// </summary>
    /// <param name="dateTime">The DateTime to check</param>
    /// <returns>True if the DateTime is in the past; otherwise, false</returns>
    public static bool IsInPast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future
    /// </summary>
    /// <param name="dateTime">The DateTime to check</param>
    /// <returns>True if the DateTime is in the future; otherwise, false</returns>
    public static bool IsInFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the start of the day for a DateTime
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>The start of the day</returns>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day for a DateTime
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>The end of the day</returns>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the start of the week for a DateTime
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <param name="startOfWeek">The start of the week (default is Monday)</param>
    /// <returns>The start of the week</returns>
    public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
        return dateTime.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Gets the start of the month for a DateTime
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>The start of the month</returns>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month for a DateTime
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>The end of the month</returns>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return dateTime.StartOfMonth().AddMonths(1).AddTicks(-1);
    }

    /// <summary>
    /// Calculates the age based on a birth date
    /// </summary>
    /// <param name="birthDate">The birth date</param>
    /// <returns>The age in years</returns>
    public static int CalculateAge(this DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        
        if (birthDate.Date > today.AddYears(-age))
            age--;
            
        return age;
    }

    /// <summary>
    /// Formats a DateTime as a relative time string (e.g., "2 hours ago")
    /// </summary>
    /// <param name="dateTime">The DateTime to format</param>
    /// <returns>The relative time string</returns>
    public static string ToRelativeTimeString(this DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        return timeSpan.TotalDays switch
        {
            > 365 => $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) == 1 ? "" : "s")} ago",
            > 30 => $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) == 1 ? "" : "s")} ago",
            > 1 => $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago",
            _ => timeSpan.TotalHours switch
            {
                > 1 => $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago",
                _ => timeSpan.TotalMinutes switch
                {
                    > 1 => $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago",
                    _ => "Just now"
                }
            }
        };
    }
}
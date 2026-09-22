using System;

namespace ActiveRest.Services;

public static class WeekMath
{
    public static DateTime StartOfWeek(DateTime date, DayOfWeek firstDay)
    {
        var daysSinceStart = ((int)date.DayOfWeek - (int)firstDay + 7) % 7;
        return date.Date.AddDays(-daysSinceStart);
    }
}

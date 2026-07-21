using BlockMachine.Models;

namespace BlockMachine.Services;

public static class ScheduleService
{
    public static bool IsBlockedNow(AppConfig config, DateTime? now = null)
    {
        if (!config.Enabled || !config.IsConfigured)
        {
            return false;
        }

        if (!TryParseTime(config.BlockStartTime, out var start) ||
            !TryParseTime(config.BlockEndTime, out var end))
        {
            return false;
        }

        var current = now ?? DateTime.Now;
        var timeOfDay = current.TimeOfDay;

        if (start == end)
        {
            return true;
        }

        if (start < end)
        {
            return timeOfDay >= start && timeOfDay < end;
        }

        return timeOfDay >= start || timeOfDay < end;
    }

    public static TimeSpan TimeUntilUnblock(AppConfig config, DateTime? now = null)
    {
        var current = now ?? DateTime.Now;

        if (!TryParseTime(config.BlockEndTime, out var end))
        {
            return TimeSpan.Zero;
        }

        var unblockAt = current.Date.Add(end);
        if (unblockAt <= current)
        {
            unblockAt = unblockAt.AddDays(1);
        }

        return unblockAt - current;
    }

    public static string FormatMessage(AppConfig config, DateTime? now = null)
    {
        var first = MessageService.GetEnabledMessages(config).FirstOrDefault();
        return first is null
            ? string.Empty
            : MessageService.FormatText(first.Body, now);
    }

    public static bool TryParseTime(string value, out TimeSpan time)
    {
        return TimeSpan.TryParseExact(
            value?.Trim(),
            ["hh\\:mm", "h\\:mm", "HH\\:mm", "H\\:mm"],
            System.Globalization.CultureInfo.InvariantCulture,
            out time);
    }

    public static bool IsValidTimeRange(string start, string end)
    {
        return TryParseTime(start, out var startTime) &&
               TryParseTime(end, out var endTime) &&
               startTime != endTime;
    }
}

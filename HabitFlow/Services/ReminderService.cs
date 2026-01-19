using Plugin.LocalNotification;

namespace HabitFlow.Services;

public static class ReminderService
{
    const int ReminderId = 1001;

    public static Task ScheduleDailyAsync(TimeSpan time, string title, string body)
    {
        // Cancel old reminder first (non-async in some versions)
        Cancel();

        var notifyAt = DateTime.Today.Add(time);
        if (notifyAt <= DateTime.Now)
            notifyAt = DateTime.Today.AddDays(1).Add(time);

        var request = new NotificationRequest
        {
            NotificationId = ReminderId,
            Title = title,
            Description = body,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyAt,
                RepeatType = NotificationRepeat.Daily
            }
        };

        // IMPORTANT:
        // In some versions Show(...) returns bool, not Task.
        LocalNotificationCenter.Current.Show(request);
        return Task.CompletedTask;
    }

    public static Task CancelAsync()
    {
        Cancel();
        return Task.CompletedTask;
    }

    static void Cancel()
    {
        // In some versions Cancel(...) returns bool, not Task.
        LocalNotificationCenter.Current.Cancel(ReminderId);
    }
}

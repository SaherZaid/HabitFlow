namespace HabitFlow.Models;

public class DaySummary
{
    public string DateKey { get; set; } = "";        // yyyy-MM-dd
    public string DisplayDate { get; set; } = "";    // e.g., Sat, Jan 17
    public int Completed { get; set; }
    public int Total { get; set; }

    public string CountText => $"{Completed}/{Total}";
    public string PercentText => Total == 0 ? "0%" : $"{(int)Math.Round((double)Completed / Total * 100)}%";
    public double ProgressValue => Total == 0 ? 0 : (double)Completed / Total;
}

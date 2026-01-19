namespace HabitFlow.Models;

public class CalendarDay
{
    public DateTime Date { get; set; }
    public bool IsInMonth { get; set; }

    public int Completed { get; set; }
    public int Total { get; set; }

    public string DayNumber => Date.Day.ToString();
    public string DateKey => Date.ToString("yyyy-MM-dd");
    public string CountText => Total == 0 ? "0/0" : $"{Completed}/{Total}";
    public double ProgressValue => Total == 0 ? 0 : (double)Completed / Total;
}

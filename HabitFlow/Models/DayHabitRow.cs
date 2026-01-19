namespace HabitFlow.Models;

public class DayHabitRow
{
    public string Name { get; set; } = "";
    public bool IsDone { get; set; }

    public string StatusIcon => IsDone ? "✅" : "⬜";
    public string StatusText => IsDone ? "Done" : "Not done";
}

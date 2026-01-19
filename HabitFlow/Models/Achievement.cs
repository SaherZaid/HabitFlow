namespace HabitFlow.Models;

public class Achievement
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "🏆";
    public bool IsUnlocked { get; set; }
}

namespace HabitFlow.Models;

public class HeatDay
{
    public string DateKey { get; set; } = "";
    public int Completed { get; set; }
    public int Total { get; set; }

    public double Ratio => Total == 0 ? 0 : (double)Completed / Total;

    public string Color
    {
        get
        {
            if (Total == 0) return "#EEEEEE";

            if (Ratio == 1) return "#2ecc71";      // full
            if (Ratio >= 0.6) return "#a8e6a3";    // good
            if (Ratio >= 0.3) return "#ffeaa7";    // medium
            if (Ratio > 0) return "#fab1a0";       // low

            return "#eeeeee";
        }
    }
}

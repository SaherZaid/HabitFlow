using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HabitFlow.Models;

public class Habit : INotifyPropertyChanged
{
    string _id = Guid.NewGuid().ToString();
    string _name = "";
    bool _isDone;
    int _streak;
    int _bestStreak;

    public string Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsDone
    {
        get => _isDone;
        set { _isDone = value; OnPropertyChanged(); }
    }

    public int Streak
    {
        get => _streak;
        set { _streak = value; OnPropertyChanged(); }
    }

    public int BestStreak
    {
        get => _bestStreak;
        set { _bestStreak = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

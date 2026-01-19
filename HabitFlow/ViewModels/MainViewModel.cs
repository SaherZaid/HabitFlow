using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HabitFlow.Models;
using HabitFlow.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace HabitFlow.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    // ---------- Storage keys ----------
    const string HabitsKey = "habits_list_v1";
    const string DailyDoneKeyPrefix = "done_";               // legacy
    const string HistoryKey = "habit_history_v1";            // { habitId: ["yyyy-MM-dd", ...] }
    const string BestStreaksKey = "best_streaks_v1";         // { habitId: bestStreak }
    const string AchievementsKey = "achievements_v1";        // ["FirstCheckmark", ...]

    const string ReminderEnabledKey = "reminder_enabled_v1";
    const string ReminderTimeKey = "reminder_time_v1";

    // ---------- Collections ----------
    public ObservableCollection<Habit> Habits { get; } = new();
    public ObservableCollection<DaySummary> HistoryDays { get; } = new();
    public ObservableCollection<CalendarDay> CalendarDays { get; } = new();
    public ObservableCollection<DayHabitRow> DayDetailsHabits { get; } = new();
    public ObservableCollection<Achievement> Achievements { get; } = new();
    public ObservableCollection<HeatDay> HeatmapDays { get; } = new();

    // ---------- Calendar state ----------
    DateTime _calendarMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public string MonthTitle => _calendarMonth.ToString("MMMM yyyy");

    public Command PrevMonthCommand { get; }
    public Command NextMonthCommand { get; }

    // ---------- History filters (optional) ----------
    public ObservableCollection<string> HistoryPresets { get; } = new()
    {
        "Last 7 days",
        "Last 14 days",
        "Last 30 days",
        "Custom"
    };

    string _selectedHistoryPreset = "Last 14 days";
    public string SelectedHistoryPreset
    {
        get => _selectedHistoryPreset;
        set
        {
            if (_selectedHistoryPreset == value) return;
            _selectedHistoryPreset = value;
            OnPropertyChanged();
            ApplyPreset(value);
        }
    }

    DateTime _historyStartDate = DateTime.Today.AddDays(-13);
    public DateTime HistoryStartDate
    {
        get => _historyStartDate;
        set { _historyStartDate = value; OnPropertyChanged(); }
    }

    DateTime _historyEndDate = DateTime.Today;
    public DateTime HistoryEndDate
    {
        get => _historyEndDate;
        set { _historyEndDate = value; OnPropertyChanged(); }
    }

    bool _historyOnlyActiveDays;
    public bool HistoryOnlyActiveDays
    {
        get => _historyOnlyActiveDays;
        set
        {
            if (_historyOnlyActiveDays == value) return;
            _historyOnlyActiveDays = value;
            OnPropertyChanged();
            RefreshHistoryDays();
        }
    }

    public Command ApplyHistoryFilterCommand { get; }

    // ---------- Today ----------
    public string TodayText => DateTime.Now.ToString("dddd, MMM d");
    public string TodayKey => DateTime.Now.ToString("yyyy-MM-dd");

    public Command AddHabitCommand { get; }
    public Command<Habit> DeleteHabitCommand { get; }
    public Command ResetTodayCommand { get; }

    // Progress UI (today)
    public int CompletedCount => Habits.Count(h => h.IsDone);
    public int TotalCount => Habits.Count;
    public string ProgressText => $"{CompletedCount} of {TotalCount} completed";
    public double ProgressValue => TotalCount == 0 ? 0 : (double)CompletedCount / TotalCount;

    // ---------- Day Details ----------
    string _dayDetailsTitle = "";
    public string DayDetailsTitle
    {
        get => _dayDetailsTitle;
        set { _dayDetailsTitle = value; OnPropertyChanged(); }
    }

    string _dayDetailsSummary = "";
    public string DayDetailsSummary
    {
        get => _dayDetailsSummary;
        set { _dayDetailsSummary = value; OnPropertyChanged(); }
    }

    public Command<string> OpenDayDetailsCommand { get; }

    // ---------- Stats ----------
    string _todayPercentText = "0%";
    public string TodayPercentText
    {
        get => _todayPercentText;
        set { _todayPercentText = value; OnPropertyChanged(); }
    }

    double _weekProgressValue;
    public double WeekProgressValue
    {
        get => _weekProgressValue;
        set { _weekProgressValue = value; OnPropertyChanged(); }
    }

    string _weekPercentText = "0%";
    public string WeekPercentText
    {
        get => _weekPercentText;
        set { _weekPercentText = value; OnPropertyChanged(); }
    }

    string _weekSummaryText = "0 / 0";
    public string WeekSummaryText
    {
        get => _weekSummaryText;
        set { _weekSummaryText = value; OnPropertyChanged(); }
    }

    string _bestHabitText = "-";
    public string BestHabitText
    {
        get => _bestHabitText;
        set { _bestHabitText = value; OnPropertyChanged(); }
    }

    // ---------- NEW: Weekly insights ----------
    string _insightsTitle = "Weekly insights";
    public string InsightsTitle
    {
        get => _insightsTitle;
        set { _insightsTitle = value; OnPropertyChanged(); }
    }

    string _bestDayText = "-";
    public string BestDayText
    {
        get => _bestDayText;
        set { _bestDayText = value; OnPropertyChanged(); }
    }

    string _worstDayText = "-";
    public string WorstDayText
    {
        get => _worstDayText;
        set { _worstDayText = value; OnPropertyChanged(); }
    }

    string _topHabitWeekText = "-";
    public string TopHabitWeekText
    {
        get => _topHabitWeekText;
        set { _topHabitWeekText = value; OnPropertyChanged(); }
    }

    string _streakLeaderText = "-";
    public string StreakLeaderText
    {
        get => _streakLeaderText;
        set { _streakLeaderText = value; OnPropertyChanged(); }
    }

    public Command ExportWeeklyPdfCommand { get; }

    // ---------- Reminders ----------
    bool _reminderEnabled;
    public bool ReminderEnabled
    {
        get => _reminderEnabled;
        set
        {
            if (_reminderEnabled == value) return;
            _reminderEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReminderPreviewText));
        }
    }

    TimeSpan _reminderTime = new(20, 0, 0);
    public TimeSpan ReminderTime
    {
        get => _reminderTime;
        set
        {
            if (_reminderTime == value) return;
            _reminderTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReminderPreviewText));
        }
    }

    public string ReminderPreviewText
    {
        get
        {
            int total = TotalCount;
            int done = CompletedCount;
            int left = Math.Max(0, total - done);

            var timeStr = DateTime.Today.Add(ReminderTime).ToString("h:mm tt");
            return ReminderEnabled
                ? $"At {timeStr}: You have {left} habits left today — keep the streak alive 🔥"
                : "Reminder is disabled.";
        }
    }

    // ✅ Saved message (shows then hides)
    string _reminderStatusText = "";
    public string ReminderStatusText
    {
        get => _reminderStatusText;
        set { _reminderStatusText = value; OnPropertyChanged(); }
    }

    bool _isReminderStatusVisible;
    public bool IsReminderStatusVisible
    {
        get => _isReminderStatusVisible;
        set { _isReminderStatusVisible = value; OnPropertyChanged(); }
    }

    public Command SaveReminderCommand { get; }
    public Command CancelReminderCommand { get; }

    // ---------- Storage ----------
    readonly Dictionary<string, HashSet<string>> _history = new();
    readonly Dictionary<string, int> _bestStreaks = new();
    readonly HashSet<string> _unlocked = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        AddHabitCommand = new Command(async () => await AddHabitAsync());
        DeleteHabitCommand = new Command<Habit>(async (h) => await DeleteHabitAsync(h));
        ResetTodayCommand = new Command(async () => await ResetTodayAsync());

        ApplyHistoryFilterCommand = new Command(() => RefreshHistoryDays());

        PrevMonthCommand = new Command(() =>
        {
            _calendarMonth = _calendarMonth.AddMonths(-1);
            OnPropertyChanged(nameof(MonthTitle));
            RefreshCalendar();
        });

        NextMonthCommand = new Command(() =>
        {
            _calendarMonth = _calendarMonth.AddMonths(1);
            OnPropertyChanged(nameof(MonthTitle));
            RefreshCalendar();
        });

        OpenDayDetailsCommand = new Command<string>(async (dateKey) =>
        {
            if (string.IsNullOrWhiteSpace(dateKey)) return;
            await Shell.Current.GoToAsync($"{nameof(HabitFlow.Pages.DayDetailsPage)}?dateKey={dateKey}");
        });

        SaveReminderCommand = new Command(async () => await SaveReminderAsync());
        CancelReminderCommand = new Command(async () => await CancelReminderAsync());

        ExportWeeklyPdfCommand = new Command(async () => await ExportWeeklyPdfAsync());

        Load();

        RaiseProgressChanged();
        RefreshHistoryDays();
        RefreshCalendar();
        RefreshStatsAndAchievements();
        RefreshHeatmap90();
        RefreshWeeklyInsights();
    }

    // ---------- Public API used by UI ----------
    public void SetHabitDone(Habit habit, bool isDone)
    {
        if (habit is null) return;

        habit.IsDone = isDone;

        var dates = GetOrCreateHistorySet(habit.Id);
        if (isDone) dates.Add(TodayKey);
        else dates.Remove(TodayKey);

        SaveTodayDoneSet();
        SaveHistory();

        habit.Streak = ComputeStreak(dates);
        UpdateBestStreak(habit);

        RaiseProgressChanged();
        RefreshHistoryDays();
        RefreshCalendar();
        RefreshStatsAndAchievements();
        RefreshHeatmap90();
        RefreshWeeklyInsights();

        if (ReminderEnabled)
            _ = SaveReminderAsync();
    }

    public void LoadDayDetails(string dateKey)
    {
        if (string.IsNullOrWhiteSpace(dateKey)) return;

        if (DateTime.TryParse(dateKey, out var dt))
            DayDetailsTitle = dt.ToString("dddd, MMM d");
        else
            DayDetailsTitle = dateKey;

        int total = Habits.Count;
        int completed = 0;

        DayDetailsHabits.Clear();

        foreach (var h in Habits)
        {
            var set = GetOrCreateHistorySet(h.Id);
            bool done = set.Contains(dateKey);
            if (done) completed++;

            DayDetailsHabits.Add(new DayHabitRow { Name = h.Name, IsDone = done });
        }

        DayDetailsSummary = $"{completed} of {total} completed";
    }

    // ---------- Add / Delete / Reset ----------
    async Task AddHabitAsync()
    {
        var name = await Application.Current!.MainPage.DisplayPromptAsync(
            "Add habit",
            "Habit name:",
            accept: "Add",
            cancel: "Cancel",
            placeholder: "e.g., Meditate 10 min");

        if (string.IsNullOrWhiteSpace(name))
            return;

        name = name.Trim();

        if (Habits.Any(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            await Application.Current!.MainPage.DisplayAlert("Already exists", "This habit is already in your list.", "OK");
            return;
        }

        var habit = new Habit { Name = name, IsDone = false, Streak = 0, BestStreak = 0 };
        Habits.Add(habit);

        GetOrCreateHistorySet(habit.Id);
        if (!_bestStreaks.ContainsKey(habit.Id))
            _bestStreaks[habit.Id] = 0;

        SaveHabits();
        SaveTodayDoneSet();
        SaveHistory();
        SaveBestStreaks();

        RaiseProgressChanged();
        RefreshHistoryDays();
        RefreshCalendar();
        RefreshStatsAndAchievements();
        RefreshHeatmap90();
        RefreshWeeklyInsights();
    }

    async Task DeleteHabitAsync(Habit? habit)
    {
        if (habit is null) return;

        bool ok = await Application.Current!.MainPage.DisplayAlert(
            "Delete habit",
            $"Delete \"{habit.Name}\"?",
            "Delete",
            "Cancel");

        if (!ok) return;

        Habits.Remove(habit);
        _history.Remove(habit.Id);
        _bestStreaks.Remove(habit.Id);

        SaveHabits();
        SaveTodayDoneSet();
        SaveHistory();
        SaveBestStreaks();

        RaiseProgressChanged();
        RefreshHistoryDays();
        RefreshCalendar();
        RefreshStatsAndAchievements();
        RefreshHeatmap90();
        RefreshWeeklyInsights();
    }

    async Task ResetTodayAsync()
    {
        if (Habits.Count == 0) return;

        bool ok = await Application.Current!.MainPage.DisplayAlert(
            "Reset today",
            "Clear all checkmarks for today?",
            "Reset",
            "Cancel");

        if (!ok) return;

        foreach (var h in Habits)
        {
            h.IsDone = false;
            var dates = GetOrCreateHistorySet(h.Id);
            dates.Remove(TodayKey);

            h.Streak = ComputeStreak(dates);
            UpdateBestStreak(h);
        }

        SaveTodayDoneSet();
        SaveHistory();
        SaveBestStreaks();

        RaiseProgressChanged();
        RefreshHistoryDays();
        RefreshCalendar();
        RefreshStatsAndAchievements();
        RefreshHeatmap90();
        RefreshWeeklyInsights();

        if (ReminderEnabled)
            _ = SaveReminderAsync();
    }

    // ---------- Load & Save ----------
    void Load()
    {
        LoadHabitsList();
        LoadHistory();
        LoadBestStreaks();
        LoadUnlockedAchievements();
        LoadReminderSettings();

        ApplyTodayDoneFromHistoryOrLegacy();
        RecomputeAllStreaksAndBest();

        HistoryEndDate = DateTime.Today;
        HistoryStartDate = DateTime.Today.AddDays(-13);
    }

    void LoadHabitsList()
    {
        Habits.Clear();

        var json = Preferences.Default.Get(HabitsKey, "");
        if (string.IsNullOrWhiteSpace(json))
        {
            Habits.Add(new Habit { Name = "Drink water" });
            Habits.Add(new Habit { Name = "Workout" });
            Habits.Add(new Habit { Name = "Read" });
            Habits.Add(new Habit { Name = "Sleep early" });

            SaveHabits();
            return;
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<Habit>>(json) ?? new List<Habit>();
            foreach (var h in list)
            {
                h.IsDone = false;
                h.Streak = 0;
                h.BestStreak = 0;
                Habits.Add(h);
            }
        }
        catch
        {
            Preferences.Default.Remove(HabitsKey);
            LoadHabitsList();
        }
    }

    void LoadHistory()
    {
        _history.Clear();

        var json = Preferences.Default.Get(HistoryKey, "");
        if (string.IsNullOrWhiteSpace(json))
        {
            foreach (var h in Habits)
                _history[h.Id] = new HashSet<string>();
            SaveHistory();
            return;
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                       ?? new Dictionary<string, List<string>>();

            foreach (var kv in dict)
                _history[kv.Key] = new HashSet<string>(kv.Value ?? new List<string>());

            foreach (var h in Habits)
                GetOrCreateHistorySet(h.Id);
        }
        catch
        {
            Preferences.Default.Remove(HistoryKey);
            LoadHistory();
        }
    }

    void LoadBestStreaks()
    {
        _bestStreaks.Clear();

        var json = Preferences.Default.Get(BestStreaksKey, "");
        if (string.IsNullOrWhiteSpace(json))
        {
            foreach (var h in Habits)
                _bestStreaks[h.Id] = 0;

            SaveBestStreaks();
            return;
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                       ?? new Dictionary<string, int>();

            foreach (var kv in dict)
                _bestStreaks[kv.Key] = kv.Value;

            foreach (var h in Habits)
                if (!_bestStreaks.ContainsKey(h.Id))
                    _bestStreaks[h.Id] = 0;
        }
        catch
        {
            Preferences.Default.Remove(BestStreaksKey);
            LoadBestStreaks();
        }
    }

    void LoadUnlockedAchievements()
    {
        _unlocked.Clear();

        var json = Preferences.Default.Get(AchievementsKey, "");
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            foreach (var a in list)
                _unlocked.Add(a);
        }
        catch
        {
            Preferences.Default.Remove(AchievementsKey);
        }
    }

    void ApplyTodayDoneFromHistoryOrLegacy()
    {
        bool anyHistoryData = _history.Values.Any(s => s.Count > 0);

        if (anyHistoryData)
        {
            foreach (var h in Habits)
            {
                var dates = GetOrCreateHistorySet(h.Id);
                h.IsDone = dates.Contains(TodayKey);
            }
            return;
        }

        var legacyJson = Preferences.Default.Get(DailyDoneKeyPrefix + TodayKey, "");
        if (string.IsNullOrWhiteSpace(legacyJson))
            return;

        try
        {
            var doneIds = JsonSerializer.Deserialize<HashSet<string>>(legacyJson) ?? new HashSet<string>();
            foreach (var h in Habits)
            {
                h.IsDone = doneIds.Contains(h.Id);

                var dates = GetOrCreateHistorySet(h.Id);
                if (h.IsDone) dates.Add(TodayKey);
            }

            SaveHistory();
        }
        catch
        {
            Preferences.Default.Remove(DailyDoneKeyPrefix + TodayKey);
        }
    }

    void SaveHabits()
    {
        var list = Habits.Select(h => new Habit { Id = h.Id, Name = h.Name }).ToList();
        var json = JsonSerializer.Serialize(list);
        Preferences.Default.Set(HabitsKey, json);
    }

    void SaveTodayDoneSet()
    {
        var doneIds = Habits.Where(h => h.IsDone).Select(h => h.Id).ToHashSet();
        var json = JsonSerializer.Serialize(doneIds);
        Preferences.Default.Set(DailyDoneKeyPrefix + TodayKey, json);
    }

    void SaveHistory()
    {
        var dict = _history.ToDictionary(k => k.Key, v => v.Value.OrderBy(x => x).ToList());
        var json = JsonSerializer.Serialize(dict);
        Preferences.Default.Set(HistoryKey, json);
    }

    void SaveBestStreaks()
    {
        var json = JsonSerializer.Serialize(_bestStreaks);
        Preferences.Default.Set(BestStreaksKey, json);
    }

    void SaveUnlockedAchievements()
    {
        var json = JsonSerializer.Serialize(_unlocked.ToList());
        Preferences.Default.Set(AchievementsKey, json);
    }

    HashSet<string> GetOrCreateHistorySet(string habitId)
    {
        if (!_history.TryGetValue(habitId, out var set))
        {
            set = new HashSet<string>();
            _history[habitId] = set;
        }
        return set;
    }

    // ---------- Streaks + Best streak ----------
    void RecomputeAllStreaksAndBest()
    {
        foreach (var h in Habits)
        {
            var dates = GetOrCreateHistorySet(h.Id);
            h.Streak = ComputeStreak(dates);

            if (_bestStreaks.TryGetValue(h.Id, out var best))
                h.BestStreak = best;
            else
                h.BestStreak = 0;

            UpdateBestStreak(h);
        }

        SaveBestStreaks();
    }

    void UpdateBestStreak(Habit h)
    {
        int current = h.Streak;

        _bestStreaks.TryGetValue(h.Id, out int best);
        if (current > best)
            best = current;

        h.BestStreak = best;
        _bestStreaks[h.Id] = best;
    }

    int ComputeStreak(HashSet<string> dates)
    {
        int streak = 0;
        var day = DateTime.Today;

        while (true)
        {
            var key = day.ToString("yyyy-MM-dd");
            if (!dates.Contains(key)) break;

            streak++;
            day = day.AddDays(-1);
        }

        return streak;
    }

    // ---------- Optional list-history ----------
    void ApplyPreset(string preset)
    {
        if (preset == "Last 7 days")
        {
            HistoryEndDate = DateTime.Today;
            HistoryStartDate = DateTime.Today.AddDays(-6);
        }
        else if (preset == "Last 14 days")
        {
            HistoryEndDate = DateTime.Today;
            HistoryStartDate = DateTime.Today.AddDays(-13);
        }
        else if (preset == "Last 30 days")
        {
            HistoryEndDate = DateTime.Today;
            HistoryStartDate = DateTime.Today.AddDays(-29);
        }

        RefreshHistoryDays();
    }

    void RefreshHistoryDays()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            HistoryDays.Clear();

            var start = HistoryStartDate.Date;
            var end = HistoryEndDate.Date;

            if (end < start)
                (start, end) = (end, start);

            for (var date = end; date >= start; date = date.AddDays(-1))
            {
                var key = date.ToString("yyyy-MM-dd");
                int total = Habits.Count;
                int completed = 0;

                foreach (var h in Habits)
                {
                    var dates = GetOrCreateHistorySet(h.Id);
                    if (dates.Contains(key))
                        completed++;
                }

                if (HistoryOnlyActiveDays && completed == 0)
                    continue;

                HistoryDays.Add(new DaySummary
                {
                    DateKey = key,
                    DisplayDate = date.ToString("ddd, MMM d"),
                    Completed = completed,
                    Total = total
                });
            }

            OnPropertyChanged(nameof(HistoryDays));
        });
    }

    // ---------- Calendar ----------
    void RefreshCalendar()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CalendarDays.Clear();

            var first = new DateTime(_calendarMonth.Year, _calendarMonth.Month, 1);
            int offset = ((int)first.DayOfWeek + 6) % 7; // Monday-first
            var start = first.AddDays(-offset);

            for (int i = 0; i < 42; i++)
            {
                var date = start.AddDays(i);
                var key = date.ToString("yyyy-MM-dd");

                int total = Habits.Count;
                int completed = 0;

                foreach (var h in Habits)
                {
                    var dates = GetOrCreateHistorySet(h.Id);
                    if (dates.Contains(key))
                        completed++;
                }

                CalendarDays.Add(new CalendarDay
                {
                    Date = date,
                    IsInMonth = date.Month == first.Month,
                    Completed = completed,
                    Total = total
                });
            }

            OnPropertyChanged(nameof(CalendarDays));
        });
    }

    // ---------- Stats + Achievements ----------
    void RefreshStatsAndAchievements()
    {
        int totalToday = Habits.Count;
        int doneToday = Habits.Count(h => h.IsDone);
        int todayPct = totalToday == 0 ? 0 : (int)Math.Round(doneToday * 100.0 / totalToday);
        TodayPercentText = $"{todayPct}%";

        var end = DateTime.Today;
        var start = end.AddDays(-6);

        int weekPossible = Habits.Count * 7;
        int weekDone = 0;

        for (var d = start; d <= end; d = d.AddDays(1))
        {
            var key = d.ToString("yyyy-MM-dd");
            foreach (var h in Habits)
            {
                var set = GetOrCreateHistorySet(h.Id);
                if (set.Contains(key))
                    weekDone++;
            }
        }

        double weekValue = weekPossible == 0 ? 0 : (double)weekDone / weekPossible;
        WeekProgressValue = weekValue;
        WeekPercentText = $"{(int)Math.Round(weekValue * 100)}%";
        WeekSummaryText = $"{weekDone} / {weekPossible}";

        if (Habits.Count == 0)
        {
            BestHabitText = "-";
        }
        else
        {
            var best = Habits
                .OrderByDescending(h => h.BestStreak)
                .ThenByDescending(h => h.Streak)
                .First();

            BestHabitText = $"{best.Name} (Best: {best.BestStreak})";
        }

        bool anyCheckmarkEver = _history.Values.Any(s => s.Count > 0);
        bool perfectDay = totalToday > 0 && doneToday == totalToday;
        int maxBest = Habits.Count == 0 ? 0 : Habits.Max(h => h.BestStreak);
        bool has7 = maxBest >= 7;
        bool has30 = maxBest >= 30;
        bool has10Habits = Habits.Count >= 10;
        bool week70 = weekValue >= 0.70;

        UnlockIf("FirstCheckmark", anyCheckmarkEver);
        UnlockIf("PerfectDay", perfectDay);
        UnlockIf("Streak7", has7);
        UnlockIf("Streak30", has30);
        UnlockIf("Habit10", has10Habits);
        UnlockIf("Week70", week70);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Achievements.Clear();

            Achievements.Add(new Achievement { Title = "First checkmark", Description = "Complete any habit at least once.", Icon = "✅", IsUnlocked = _unlocked.Contains("FirstCheckmark") });
            Achievements.Add(new Achievement { Title = "Perfect day", Description = "Complete all habits in a single day.", Icon = "💯", IsUnlocked = _unlocked.Contains("PerfectDay") });
            Achievements.Add(new Achievement { Title = "7-day streak", Description = "Reach a 7-day streak on any habit.", Icon = "🔥", IsUnlocked = _unlocked.Contains("Streak7") });
            Achievements.Add(new Achievement { Title = "30-day streak", Description = "Reach a 30-day streak on any habit.", Icon = "🏆", IsUnlocked = _unlocked.Contains("Streak30") });
            Achievements.Add(new Achievement { Title = "Habit builder", Description = "Create 10 habits.", Icon = "🧱", IsUnlocked = _unlocked.Contains("Habit10") });
            Achievements.Add(new Achievement { Title = "Consistent week", Description = "Hit 70%+ completion in the last 7 days.", Icon = "📈", IsUnlocked = _unlocked.Contains("Week70") });

            OnPropertyChanged(nameof(Achievements));
        });
    }

    void UnlockIf(string key, bool condition)
    {
        if (!condition) return;
        if (_unlocked.Contains(key)) return;

        _unlocked.Add(key);
        SaveUnlockedAchievements();
    }

    // ---------- Heatmap ----------
    void RefreshHeatmap90()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            HeatmapDays.Clear();

            var end = DateTime.Today;
            var start = end.AddDays(-89);

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var key = d.ToString("yyyy-MM-dd");

                int total = Habits.Count;
                int completed = 0;

                foreach (var h in Habits)
                {
                    var set = GetOrCreateHistorySet(h.Id);
                    if (set.Contains(key))
                        completed++;
                }

                HeatmapDays.Add(new HeatDay { DateKey = key, Completed = completed, Total = total });
            }

            OnPropertyChanged(nameof(HeatmapDays));
        });
    }

    // ---------- Weekly Insights ----------
    void RefreshWeeklyInsights()
    {
        var end = DateTime.Today;
        var start = end.AddDays(-6);

        InsightsTitle = $"Weekly insights ({start:MMM d} - {end:MMM d})";

        (DateTime day, int completed, int total, double ratio) best = (start, 0, 0, -1);
        (DateTime day, int completed, int total, double ratio) worst = (start, 0, 0, 2);

        for (var d = start; d <= end; d = d.AddDays(1))
        {
            var key = d.ToString("yyyy-MM-dd");
            int total = Habits.Count;
            int completed = 0;

            foreach (var h in Habits)
            {
                var set = GetOrCreateHistorySet(h.Id);
                if (set.Contains(key))
                    completed++;
            }

            double ratio = total == 0 ? 0 : (double)completed / total;

            if (ratio > best.ratio) best = (d, completed, total, ratio);
            if (ratio < worst.ratio) worst = (d, completed, total, ratio);
        }

        BestDayText = Habits.Count == 0 ? "-" : $"{best.day:ddd, MMM d} — {(int)Math.Round(best.ratio * 100)}% ({best.completed}/{best.total})";
        WorstDayText = Habits.Count == 0 ? "-" : $"{worst.day:ddd, MMM d} — {(int)Math.Round(worst.ratio * 100)}% ({worst.completed}/{worst.total})";

        if (Habits.Count == 0)
        {
            TopHabitWeekText = "-";
            StreakLeaderText = "-";
            return;
        }

        var habitWeekCounts = new List<(Habit habit, int count)>();
        foreach (var h in Habits)
        {
            int count = 0;
            var set = GetOrCreateHistorySet(h.Id);

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var key = d.ToString("yyyy-MM-dd");
                if (set.Contains(key)) count++;
            }

            habitWeekCounts.Add((h, count));
        }

        var top = habitWeekCounts.OrderByDescending(x => x.count).ThenByDescending(x => x.habit.BestStreak).First();
        TopHabitWeekText = $"{top.habit.Name} — {top.count}/7 days";

        var leader = Habits.OrderByDescending(h => h.Streak).ThenByDescending(h => h.BestStreak).First();
        StreakLeaderText = $"{leader.Name} — Current {leader.Streak}, Best {leader.BestStreak}";
    }

    // ---------- Reminders ----------
    void LoadReminderSettings()
    {
        ReminderEnabled = Preferences.Default.Get(ReminderEnabledKey, false);

        var timeStr = Preferences.Default.Get(ReminderTimeKey, "20:00");
        if (TimeSpan.TryParse(timeStr, out var t))
            ReminderTime = t;
        else
            ReminderTime = new TimeSpan(20, 0, 0);
    }

    void SaveReminderSettings()
    {
        Preferences.Default.Set(ReminderEnabledKey, ReminderEnabled);
        Preferences.Default.Set(ReminderTimeKey, ReminderTime.ToString(@"hh\:mm"));
    }

    async Task ShowReminderStatusAsync(string text)
    {
        // Always update UI on main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReminderStatusText = text;
            IsReminderStatusVisible = true;
        });

        await Task.Delay(2000);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsReminderStatusVisible = false;
            ReminderStatusText = "";
        });
    }

    async Task SaveReminderAsync()
    {
        try
        {
            SaveReminderSettings();

            if (!ReminderEnabled)
            {
                await ReminderService.CancelAsync();
                await ShowReminderStatusAsync("Saved ✅ (disabled)");
                return;
            }

            int total = TotalCount;
            int done = CompletedCount;
            int left = Math.Max(0, total - done);

            var title = "HabitFlow";
            var body = left == 0
                ? "Perfect day! Keep it going 💯"
                : $"You have {left} habits left today — keep the streak alive 🔥";

            await ReminderService.ScheduleDailyAsync(ReminderTime, title, body);
            await ShowReminderStatusAsync("Saved & scheduled ✅");
        }
        catch
        {
            await ShowReminderStatusAsync("Failed ❌");
        }
    }

    async Task CancelReminderAsync()
    {
        try
        {
            ReminderEnabled = false;
            SaveReminderSettings();
            await ReminderService.CancelAsync();

            await ShowReminderStatusAsync("Canceled ✅");
        }
        catch
        {
            await ShowReminderStatusAsync("Failed ❌");
        }
    }

    // ---------- Export Weekly PDF ----------
    async Task ExportWeeklyPdfAsync()
    {
        try
        {
            var end = DateTime.Today;
            var start = end.AddDays(-6);

            int weekPossible = Habits.Count * 7;
            int weekDone = 0;

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var key = d.ToString("yyyy-MM-dd");
                foreach (var h in Habits)
                {
                    var set = GetOrCreateHistorySet(h.Id);
                    if (set.Contains(key))
                        weekDone++;
                }
            }

            int pct = weekPossible == 0 ? 0 : (int)Math.Round(weekDone * 100.0 / weekPossible);

            var lines = new List<string>
            {
                "HabitFlow — Weekly Report",
                $"{start:MMM d, yyyy} - {end:MMM d, yyyy}",
                "",
                $"Completion: {pct}% ({weekDone}/{weekPossible})",
                $"Best day: {BestDayText}",
                $"Worst day: {WorstDayText}",
                $"Top habit: {TopHabitWeekText}",
                $"Streak leader: {StreakLeaderText}",
                "",
                "Keep going 🔥"
            };

            byte[] pdf = SimplePdfOnePage(lines);

            var fileName = $"HabitFlow_WeeklyReport_{end:yyyyMMdd}.pdf";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            File.WriteAllBytes(path, pdf);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share weekly report",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage.DisplayAlert("Export failed", ex.Message, "OK");
        }
    }

    static byte[] SimplePdfOnePage(IReadOnlyList<string> lines)
    {
        string Safe(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(c <= 127 ? c : '?');
            return sb.ToString();
        }

        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 16 Tf");
        content.AppendLine("72 760 Td");

        int lineHeight = 18;
        bool first = true;

        foreach (var raw in lines)
        {
            var s = Safe(raw);

            if (!first)
                content.AppendLine($"0 -{lineHeight} Td");
            first = false;

            s = s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            content.AppendLine($"({s}) Tj");
        }

        content.AppendLine("ET");
        var contentBytes = Encoding.ASCII.GetBytes(content.ToString());

        var objects = new List<byte[]>();
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));
        objects.Add(Encoding.ASCII.GetBytes($"<< /Length {contentBytes.Length} >>\nstream\n{Encoding.ASCII.GetString(contentBytes)}\nendstream"));

        var pdf = new MemoryStream();
        void W(string s) => pdf.Write(Encoding.ASCII.GetBytes(s));

        W("%PDF-1.4\n");

        var xrefPositions = new List<long> { 0 };
        for (int i = 0; i < objects.Count; i++)
        {
            xrefPositions.Add(pdf.Position);
            W($"{i + 1} 0 obj\n");
            pdf.Write(objects[i]);
            W("\nendobj\n");
        }

        long xrefStart = pdf.Position;
        W("xref\n");
        W($"0 {objects.Count + 1}\n");
        W("0000000000 65535 f \n");

        for (int i = 1; i < xrefPositions.Count; i++)
            W($"{xrefPositions[i]:0000000000} 00000 n \n");

        W("trailer\n");
        W($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        W("startxref\n");
        W($"{xrefStart}\n");
        W("%%EOF");

        return pdf.ToArray();
    }

    void RaiseProgressChanged()
    {
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressValue));
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

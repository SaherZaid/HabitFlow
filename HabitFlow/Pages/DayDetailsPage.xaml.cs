using HabitFlow.ViewModels;

namespace HabitFlow.Pages;

[QueryProperty(nameof(DateKey), "dateKey")]
public partial class DayDetailsPage : ContentPage
{
    string _dateKey = "";

    public string DateKey
    {
        get => _dateKey;
        set
        {
            _dateKey = value ?? "";
            Load();
        }
    }

    public DayDetailsPage()
    {
        InitializeComponent();
        BindingContext = ((HabitFlow.App)Application.Current!).MainVm;
    }

    void Load()
    {
        if (BindingContext is not MainViewModel vm) return;
        if (string.IsNullOrWhiteSpace(DateKey)) return;

        vm.LoadDayDetails(DateKey);
    }
}

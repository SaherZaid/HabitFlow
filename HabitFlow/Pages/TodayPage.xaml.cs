using HabitFlow.Models;
using HabitFlow.ViewModels;

namespace HabitFlow.Pages;

public partial class TodayPage : ContentPage
{
    public TodayPage()
    {
        InitializeComponent();

        // Get the shared VM from App
        BindingContext = ((HabitFlow.App)Application.Current!).MainVm;
    }

    void OnHabitCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (BindingContext is not MainViewModel vm) return;
        if (sender is not CheckBox cb) return;
        if (cb.BindingContext is not Habit habit) return;

        vm.SetHabitDone(habit, e.Value);
    }
}

using CommunityToolkit.Maui.Views;
using System.Text.Json;

namespace Aquardium;

public partial class FeederTimePopup : Popup<string>
{
    public FeederTimePopup()
    {
        InitializeComponent();
        PeriodPicker.SelectedIndex = -1;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!int.TryParse(HourEntry.Text, out int hour) || hour < 1 || hour > 12)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Hour must be between 1 and 12.", "OK");
            return;
        }

        if (!int.TryParse(MinuteEntry.Text, out int minute) || minute < 0 || minute > 59)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Minute must be between 0 and 59.", "OK");
            return;
        }

        if (PeriodPicker.SelectedIndex == -1)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please select AM or PM.", "OK");
            return;
        }

        if (PeriodPicker.SelectedItem.ToString() == "PM")
            hour += 12;
        else if (PeriodPicker.SelectedItem.ToString() == "AM" && hour == 12)
            hour = 0;

        var jsonResult = JsonSerializer.Serialize(new
        {
            Hour = hour,
            Minute = minute
        });

        await CloseAsync(jsonResult);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseAsync(null);
    }
}

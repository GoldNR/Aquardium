using CommunityToolkit.Maui.Views;
using System.Text.Json;

namespace Aquardium;

public partial class FeederTimePopup : Popup<string>
{
    private readonly Dictionary<string, Entry> hourEntries = new();
    private readonly Dictionary<string, Entry> minuteEntries = new();
    private readonly Dictionary<string, Picker> periodPickers = new();
    private int count = 1; // update when combo box changes

    public FeederTimePopup()
    {
        InitializeComponent();
    }

    private void OnFeedCountChanged(object sender, EventArgs e)
    {
        if (FeedCountPicker.SelectedItem == null)
            return;

        count = int.Parse(FeedCountPicker.SelectedItem.ToString());
        TimeInputsContainer.Children.Clear();

        for (int i = 1; i <= count; i++)
        {
            var hourEntry = new Entry
            {
                Placeholder = "HH",
                Keyboard = Keyboard.Numeric,
                WidthRequest = 60,
                TextColor = Colors.White
            };

            var minuteEntry = new Entry
            {
                Placeholder = "MM",
                Keyboard = Keyboard.Numeric,
                WidthRequest = 60,
                TextColor = Colors.White
            };

            var periodPicker = new Picker
            {
                WidthRequest = 60,
                ItemsSource = new List<string> { "AM", "PM" }
            };

            var row = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center
            };

            row.Add(hourEntry);
            row.Add(minuteEntry);
            row.Add(periodPicker);

            hourEntries[$"Hour{i}"] = hourEntry;
            minuteEntries[$"Minute{i}"] = minuteEntry;
            periodPickers[$"Period{i}"] = periodPicker;

            // Optional label to indicate which feeding
            var label = new Label
            {
                Text = $"Feeding {i}",
                TextColor = Colors.LightGray,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center
            };

            TimeInputsContainer.Add(label);
            TimeInputsContainer.Add(row);
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var feedTimes = new List<Dictionary<string, int>>();

        for (int i = 1; i <= count; i++)
        {
            var hourEntry = hourEntries[$"Hour{i}"];
            var minuteEntry = minuteEntries[$"Minute{i}"];
            var periodPicker = periodPickers[$"Period{i}"];

            if (!int.TryParse(hourEntry.Text, out int hour) || hour < 1 || hour > 12)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Hour {i} must be between 1 and 12.", "OK");
                return;
            }

            if (!int.TryParse(minuteEntry.Text, out int minute) || minute < 0 || minute > 59)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Minute {i} must be between 0 and 59.", "OK");
                return;
            }

            if (periodPicker.SelectedIndex == -1)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Please select AM or PM for time {i}.", "OK");
                return;
            }

            string period = periodPicker.SelectedItem.ToString();

            // Convert to 24-hour format
            if (period == "PM" && hour < 12)
                hour += 12;
            else if (period == "AM" && hour == 12)
                hour = 0;

            feedTimes.Add(new Dictionary<string, int>
            {
                { $"hour{i}", hour },
                { $"minute{i}", minute }
            });
        }

        var jsonResult = JsonSerializer.Serialize(feedTimes);

        await CloseAsync(jsonResult);
    }


    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseAsync(null);
    }
}

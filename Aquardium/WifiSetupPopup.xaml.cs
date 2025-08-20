using CommunityToolkit.Maui.Views;
using System.Text.Json;

namespace Aquardium;

public partial class WifiSetupPopup : Popup<string>
{
	public WifiSetupPopup()
	{
		InitializeComponent();
	}

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SsidEntry.Text))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "SSID cannot be empty.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Password cannot be empty.", "OK");
            return;
        }
        var jsonResult = JsonSerializer.Serialize(new
        {
            Ssid = SsidEntry.Text,
            Password = PasswordEntry.Text
        });
        await CloseAsync(jsonResult);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseAsync(null);
    }
}
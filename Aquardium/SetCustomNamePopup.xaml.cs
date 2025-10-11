using System.Text.Json;
using CommunityToolkit.Maui.Views;

namespace Aquardium;

public partial class SetCustomNamePopup : Popup<string>
{
	public SetCustomNamePopup()
	{
		InitializeComponent();
	}

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Name cannot be blank.", "OK");
            return;
        }
        await CloseAsync(NameEntry.Text);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseAsync(null);
    }
}
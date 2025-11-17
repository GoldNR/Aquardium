using CommunityToolkit.Maui.Views;

namespace Aquardium;

public partial class SetPHPopup : Popup<string>
{
	public SetPHPopup()
	{
		InitializeComponent();
	}

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PHEntry.Text))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Name cannot be blank.", "OK");
            return;
        }
        await CloseAsync(PHEntry.Text);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseAsync(null);
    }
}
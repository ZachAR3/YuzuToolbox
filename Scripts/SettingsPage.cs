using Godot;


public partial class SettingsPage : Node
{
    [Export()] private ModManager _modManager;
    [Export()] private LineEdit _githubTokenLineEdit;
    [Export()] private Tools _tools;

    [Export()] private CheckBox _getCompatibleVersionsButton;


    private void Initiate()
    {
        _getCompatibleVersionsButton.ButtonPressed = Globals.Instance.Settings.GetCompatibleVersions;
    }
    
    
    // Signal functions
    private async void ResetSettingsPressed()
    {
        var confirm = await _tools.ConfirmationPopup();
        if (confirm != true)
        {
            return;
        }
        Globals.Instance.Settings = new SettingsResource();
        Globals.Instance.SetDefaultPaths();
    }


    private async void ResetInstalledModsPressed()
    {
        var confirm = await _tools.ConfirmationPopup();
        if (confirm != true)
        {
            return;
        }
        _modManager.ResetInstalled();
    }


    private void GithubTokenEntered(string token)
    {
        Globals.Instance.Settings.GithubApiToken = token;
        Globals.Instance.SaveManager.WriteSave();
        Globals.Instance.AuthenticateGithubClient();
        _githubTokenLineEdit.Text = "Success!";
    }


    private void GetCompatibleToggled(bool getCompatible)
    {
        Globals.Instance.Settings.GetCompatibleVersions = getCompatible;
        Globals.Instance.SaveManager.WriteSave();
    }
}

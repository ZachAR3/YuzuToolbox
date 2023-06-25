using Godot;
using System;

public partial class SettingsPage : Node
{
    [Export()] private ModManager _modManager;
    [Export()] private LineEdit _githubTokenLineEdit;

    private Tools _tools = new();
    
    
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
}

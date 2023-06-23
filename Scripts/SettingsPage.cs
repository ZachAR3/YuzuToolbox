using Godot;
using System;

public partial class SettingsPage : Node
{
    [Export()] private ModManager _modManager;
    [Export()] private PopupMenu _confirmationPopup;

    private Tools _tools = new();
    
    
    // Signal functions
    private async void ResetSettingsPressed()
    {
        var confirm = await _tools.ConfirmationPopup(_confirmationPopup);
        if (confirm != true)
        {
            return;
        }
        Globals.Instance.Settings = new SettingsResource();
        Globals.Instance.SetDefaultPaths();
    }


    private async void ResetInstalledModsPressed()
    {
        var confirm = await _tools.ConfirmationPopup(_confirmationPopup);
        if (confirm != true)
        {
            return;
        }
        _modManager.ResetInstalled();
    }
}

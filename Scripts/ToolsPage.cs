using Godot;
using System;
using System.Threading.Tasks;

public partial class ToolsPage : Control
{
	[ExportGroup("General")]
	[Export()] private Popup _errorPopup;
	[Export()] private Label _errorLabel;
	[Export()] private PopupMenu _confirmationPopup;
	
	[ExportGroup("Tools")] 
	[Export()] private Button _clearInstallFolderButton;
	[Export()] private Button _clearShadersToolButton;
	[Export()] private Button _backupSavesButton;
	[Export()] private Button _restoreSavesButton;
	[Export()] private Button _fromSaveDirectoryButton;
	[Export()] private Button _toSaveDirectoryButton;
	
	private Tools _tools = new Tools();
	private string _osUsed = OS.GetName();
	
	
	// Godot Functions
	public override void _Ready()
	{
		if (Globals.Instance.Settings.FromSaveDirectory == null)
		{
			Globals.Instance.Settings.FromSaveDirectory = _osUsed == "Linux"
				? $@"{Globals.Instance.Settings.AppDataPath}nand/user/save"
				: $@"{Globals.Instance.Settings.AppDataPath}nand\user\save";
			Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
		}
		
		_fromSaveDirectoryButton.Text = Globals.Instance.Settings.FromSaveDirectory;
		_toSaveDirectoryButton.Text = Globals.Instance.Settings.ToSaveDirectory;
	}
	
	
	// Signal functions
	private async void ClearInstallFolderButtonPressed()
	{
		var confirm = await _tools.ConfirmationPopup(_confirmationPopup);
		if (confirm == false)
		{
			return;
		}
		
		// Clears the install folder, if failed notifies user
		if (!_tools.ClearInstallationFolder(Globals.Instance.Settings.SaveDirectory))
		{
			_tools.ErrorPopup("failed to clear installation folder", _errorLabel, _errorPopup);
			_clearInstallFolderButton.Text = "Clear failed!";
		}
		else
		{
			_clearInstallFolderButton.Text = "Cleared successfully!";
		}
	}
	
	
	
	private async void ClearShaderButtonPressed()
	{
		var confirm = await _tools.ConfirmationPopup(_confirmationPopup);
		if (confirm == false)
		{
			return;
		}
		
		// Clears the shaders, if returned an error notifies user.
		if (!_tools.ClearShaders(Globals.Instance.Settings.ShadersLocation))
		{
			_tools.ErrorPopup("failed to clear shaders", _errorLabel, _errorPopup);
			_clearShadersToolButton.Text = "Clear failed!";
		}
		else
		{
			_clearShadersToolButton.Text = "Cleared successfully!";
		}
	}
	
	
	private void OnBackupSavesButtonPressed()
	{
		try
		{
			_tools.DuplicateDirectoryContents(Globals.Instance.Settings.FromSaveDirectory, Globals.Instance.Settings.ToSaveDirectory, true);
			_backupSavesButton.Text = "Backup successful!";
		}
		catch (Exception backupError)
		{ 
			_tools.ErrorPopup("failed to create save backup exception:" + backupError, _errorLabel, _errorPopup);
			throw;
		}

	}


	private void OnRestoreSavesPressed()
	{
		try
		{
			_tools.DuplicateDirectoryContents(Globals.Instance.Settings.ToSaveDirectory, Globals.Instance.Settings.FromSaveDirectory, true);
			_restoreSavesButton.Text = "Saves restored successfully!";
		}
		catch (Exception restoreError)
		{
			_tools.ErrorPopup("failed to restore saves, exception: " + restoreError, _errorLabel, _errorPopup);
			throw;
		}
	}
	
	
	private void OnFromSaveDirectoryButtonPressed()
	{
		var fromSaveDirectoryInput = _tools
			.OpenFileChooser(Globals.Instance.Settings.FromSaveDirectory, _errorLabel, _errorPopup).Result;
		if (fromSaveDirectoryInput != null)
		{
			Globals.Instance.Settings.FromSaveDirectory = fromSaveDirectoryInput;
		}

		_fromSaveDirectoryButton.Text = Globals.Instance.Settings.FromSaveDirectory;
		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
	}
	
	private void OnToSaveDirectoryButtonPressed()
	{
		var toSaveDirectoryInput = _tools
			.OpenFileChooser(Globals.Instance.Settings.ToSaveDirectory, _errorLabel, _errorPopup).Result;
		if (toSaveDirectoryInput != null)
		{
			Globals.Instance.Settings.ToSaveDirectory = toSaveDirectoryInput;
		}

		_toSaveDirectoryButton.Text = Globals.Instance.Settings.ToSaveDirectory;
		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
	}


}

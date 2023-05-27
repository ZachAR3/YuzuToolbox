using Godot;
using System;

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

	private ResourceSaveManager _saveManager = new ResourceSaveManager();
	private SettingsResource _settings;
	private Tools _tools = new Tools();
	private string _osUsed = OS.GetName();
	
	
	// Functions
	public override void _Ready()
	{
		_settings = _saveManager.GetSettings();
		if (_settings.ShadersLocation == "")
		{
			_settings.ShadersLocation = $@"{_settings.AppDataPath}shader";
			_saveManager.WriteSave(_settings);
		}

		if (_settings.FromSaveDirectory == "")
		{
			_settings.FromSaveDirectory = _osUsed == "Linux"
				? $@"{_settings.AppDataPath}nand/user/save"
				: $@"{_settings.AppDataPath}nand\user\save";
			_saveManager.WriteSave(_settings);
		}
		
		_fromSaveDirectoryButton.Text = _settings.FromSaveDirectory;
		_toSaveDirectoryButton.Text = _settings.ToSaveDirectory;
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
		if (!_tools.ClearInstallationFolder(_settings.SaveDirectory))
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
		if (!_tools.ClearShaders(_settings.ShadersLocation))
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
			_tools.DuplicateDirectoryContents(_settings.FromSaveDirectory, _settings.ToSaveDirectory, true);
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
			_tools.DuplicateDirectoryContents(_settings.ToSaveDirectory, _settings.FromSaveDirectory, true);
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
		_tools.OpenFileChooser(ref _settings.FromSaveDirectory, _settings.FromSaveDirectory, _errorLabel, _errorPopup);
		_fromSaveDirectoryButton.Text = _settings.FromSaveDirectory;
		_saveManager.WriteSave(_settings);
	}
	
	private void OnToSaveDirectoryButtonPressed()
	{
		_tools.OpenFileChooser(ref _settings.ToSaveDirectory, _settings.ToSaveDirectory, _errorLabel, _errorPopup);
		_toSaveDirectoryButton.Text = _settings.ToSaveDirectory;
		_saveManager.WriteSave(_settings);
	}


}

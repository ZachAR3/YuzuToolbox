using Godot;
using System;
using NativeFileDialogSharp;

public partial class ToolsPage : Control
{
	[ExportGroup("Tools")] 
	[Export()] private Button _clearInstallFolderButton;
	[Export()] private Button _clearShadersToolButton;
	[Export()] private Button _backupSavesButton;
	[Export()] private Button _restoreSavesButton;
	[Export()] private Button _fromSaveDirectoryButton;
	[Export()] private Button _toSaveDirectoryButton;
	
	private string _osUsed = OS.GetName();
	
	
	// Godot Functions
	private void Initiate()
	{
		_fromSaveDirectoryButton.Text = Globals.Instance.Settings.FromSaveDirectory;
		_toSaveDirectoryButton.Text = Globals.Instance.Settings.ToSaveDirectory;
	}
	
	
	// Signal functions
	private async void ClearInstallFolderButtonPressed()
	{
		var confirm = await Tools.Instance.ConfirmationPopup();
		if (confirm == false)
		{
			return;
		}
		
		// Clears the install folder, if failed notifies user
		if (!Tools.Instance.ClearInstallationFolder(Globals.Instance.Settings.SaveDirectory))
		{
			Tools.Instance.AddError("failed to clear installation folder");
			_clearInstallFolderButton.Text = "Clear failed!";
		}
		else
		{
			_clearInstallFolderButton.Text = "Cleared successfully!";
		}
	}
	
	
	
	private async void ClearShaderButtonPressed()
	{
		var confirm = await Tools.Instance.ConfirmationPopup();
		if (confirm == false)
		{
			return;
		}
		
		// Clears the shaders, if returned an error notifies user.
		if (!Tools.Instance.ClearShaders(Globals.Instance.Settings.ShadersLocation))
		{
			Tools.Instance.AddError("failed to clear shaders");
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
			Tools.Instance.DuplicateDirectoryContents(Globals.Instance.Settings.FromSaveDirectory, Globals.Instance.Settings.ToSaveDirectory, true);
			_backupSavesButton.Text = "Backup successful!";
		}
		catch (Exception backupError)
		{ 
			Tools.Instance.AddError("failed to create save backup exception:" + backupError);
			throw;
		}

	}


	private void OnRestoreSavesPressed()
	{
		try
		{
			Tools.Instance.DuplicateDirectoryContents(Globals.Instance.Settings.ToSaveDirectory, Globals.Instance.Settings.FromSaveDirectory, true);
			_restoreSavesButton.Text = "Saves restored successfully!";
		}
		catch (Exception restoreError)
		{
			Tools.Instance.AddError("failed to restore saves, exception: " + restoreError);
			throw;
		}
	}
	
	
	private void OnFromSaveDirectoryButtonPressed()
	{
		var fromSaveDirectoryInput = Dialog.FolderPicker(Globals.Instance.Settings.FromSaveDirectory).Path;
		if (fromSaveDirectoryInput != null)
		{
			Globals.Instance.Settings.FromSaveDirectory = fromSaveDirectoryInput;
		}

		_fromSaveDirectoryButton.Text = Globals.Instance.Settings.FromSaveDirectory;
		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
	}
	
	private void OnToSaveDirectoryButtonPressed()
	{
		var toSaveDirectoryInput = Dialog.FolderPicker(Globals.Instance.Settings.ToSaveDirectory).Path;
		if (toSaveDirectoryInput != null)
		{
			Globals.Instance.Settings.ToSaveDirectory = toSaveDirectoryInput;
		}

		_toSaveDirectoryButton.Text = Globals.Instance.Settings.ToSaveDirectory;
		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
	}


}

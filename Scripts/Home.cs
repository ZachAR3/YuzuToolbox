using Godot;
using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using Gtk;
using FileAccess = Godot.FileAccess;
using ProgressBar = Godot.ProgressBar;


public partial class Home : Control
{
	[Export()] private OptionButton _versionButton;
	[Export()] private Godot.Button _locationButton;
	[Export()] private Godot.Button _downloadButton;
	[Export()] private ProgressBar _downloadProgressBar;
	[Export()] private Timer _downloadUpdateTimer;
	[Export()] private Popup _errorPopup;
	[Export()] private Godot.Label _errorLabel;
	[Export()] private HttpRequest _latestReleaseRequester;
	[Export()] private HttpRequest _downloadRequester;
	[Export()] private String _pineappleLatestUrl;
	[Export()] private String _pineappleDownloadBaseUrl;
	[Export()] private string _yuzuBaseString = "Yuzu-EA-";
	[Export()] private string _saveName;
	[Export()] private int _previousVersionsToAdd = 10;

	private FileChooserDialog _fileChooser;
	private ResourceSaveManager _saveManager;
	private SettingsResource _settings;
	private String _osUsed;
	private string _yuzuExtensionString;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_osUsed = "Windows"; //OS.GetName();
		if (_osUsed == "Linux")
		{
			_saveName += ".AppImage";
			_yuzuExtensionString = ".AppImage";
		}
		else
		{
			_saveName += ".zip";
			_yuzuExtensionString = ".zip";
		}
		
		_saveManager = new ResourceSaveManager();
		GetSettings();
		_settings.SaveDirectory = _settings.SaveDirectory;
		_locationButton.Text = _settings.SaveDirectory;
		
		// Call a request to get the latest versions and connect it to our GetNewVersions function
		_latestReleaseRequester.RequestCompleted += AddVersions;
		_latestReleaseRequester.Request(_pineappleLatestUrl);

		_downloadButton.Pressed += InstallSelectedVersion;
		_locationButton.Pressed += OpenFileChooser;
		_downloadRequester.RequestCompleted += VersionDownloadCompleted;
		_downloadUpdateTimer.Timeout += UpdateDownloadBar;
	}


	private void InstallSelectedVersion()
	{
		DeleteOldVersion();
		
		// Set old install (if it exists) to not be disabled anymore.
		if (_settings.InstalledVersion != -1)
		{
			_versionButton.SetItemDisabled(_versionButton.GetItemIndex(_settings.InstalledVersion), false);
		}
		
		int versionIndex = _versionButton.Selected;
		int version = _versionButton.GetItemText(versionIndex).ToInt();
		_settings.InstalledVersion = version;
		_downloadButton.Text = "Downloading...";
		_downloadRequester.DownloadFile = _settings.SaveDirectory + "/" + _saveName;
		_downloadRequester.Request(_pineappleDownloadBaseUrl + version + "/" + _osUsed + "-" + _yuzuBaseString + version + _yuzuExtensionString);
		_downloadUpdateTimer.Start();
	}


	private void VersionDownloadCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_downloadUpdateTimer.Stop();
		if (result == (int)HttpRequest.Result.Success)
		{
			_saveManager._settings = _settings;
			_saveManager.WriteSave();
			_downloadProgressBar.Value = 100;
			_downloadButton.Text = "Successfully Downloaded!";
			
			AddInstalledVersion();
		}
		else
		{
			_downloadProgressBar.Value = 0;
		}
	}
	
	
	private void UpdateDownloadBar()
	{
		_downloadProgressBar.Value = (float)_downloadRequester.GetDownloadedBytes()/_downloadRequester.GetBodySize() * 100;
	}
	

	private void AddVersions(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result == (int)HttpRequest.Result.Success)
		{
			int latestVersion = GetLatestVersion(Encoding.UTF8.GetString(body));

			//Add a version item for the latest and the dictated amount of previous versions.
			for (int previousIndex = 0; previousIndex < _previousVersionsToAdd; previousIndex++)
			{
				_versionButton.AddItem((latestVersion-previousIndex).ToString(), latestVersion-previousIndex);
			}

			//Checks if there is already a version installed, and if so adds it to the list
			if (_settings.InstalledVersion != -1)
			{
				AddInstalledVersion();
			}
		}
		else
		{
			ErrorPopup("Failed to get latest versions error code: " + responseCode);
		}
	}


	private void AddInstalledVersion()
	{
		var installedVersion = _settings.InstalledVersion;
		var selectedIndex = _versionButton.GetItemIndex(installedVersion);
				
		// Checks if the item was already added, if so sets it as current, otherwise adds a new item entry for it.
		if (selectedIndex > 0)
		{
			_versionButton.Selected = selectedIndex;
		}
		else
		{
			_versionButton.AddItem(installedVersion.ToString(), installedVersion);
			selectedIndex = _versionButton.GetItemIndex(installedVersion);
			_versionButton.Selected = selectedIndex;
		}
		_versionButton.SetItemDisabled(selectedIndex, true);
	}


	private int GetLatestVersion(String rawVersionData)
	{
		string searchName = _osUsed + "-" + _yuzuBaseString;
		int versionIndex = rawVersionData.Find(searchName);
		//GD.Print(versionIndex);

		// Using our starting index subtract the index of our extension from it and add 1 to get the length of the version
		int versionLength =  rawVersionData.Find(_yuzuExtensionString) -versionIndex -searchName.Length;
		//GD.Print(versionLength);
		
		// Return version by starting at our start index (accounting for our search string) and going the previously determined length
		return rawVersionData.Substring(versionIndex + searchName.Length, versionLength).ToInt();
	}


	private void GetSettings()
	{
		if (ResourceSaveManager.SaveExists())
		{
			var lastSave = (ResourceSaveManager)ResourceSaveManager.LoadSaveGame();
			if (lastSave != null)
			{
				_settings = lastSave._settings;
			}
			else
			{
				ErrorPopup("Error loading settings, please delete and regenerate settings file.");
			}
		}
		else
		{
			_settings = new SettingsResource();
			_saveManager._settings = _settings;
			_saveManager.WriteSave();
		}
	}

	private String GetExistingVersion()
	{
		if (DirAccess.DirExistsAbsolute(_settings.SaveDirectory))
		{
			var previousSave = DirAccess.Open(_settings.SaveDirectory);

			foreach (var file in previousSave.GetFiles())
			{
				if (file.GetExtension() == "AppImage" || file.GetBaseName() == "exe")
				{
					return _settings.SaveDirectory + "/" + file;
				}
			}
		}	

		return "";
	}


	private void DeleteOldVersion()
	{
		var existingVersionLocation = GetExistingVersion();
		if (existingVersionLocation == "")
		{
			return;
		}
		
		var deleteError = DirAccess.RemoveAbsolute(existingVersionLocation);
		if (deleteError != Error.Ok)
		{
			ErrorPopup("Error deleting old version, file not found.");
		}
	}
	

	private void OpenFileChooser()
	{
		Application.Init();
		_fileChooser = new FileChooserDialog("Select a File", null, FileChooserAction.SelectFolder);

		// Add a "Cancel" button to the dialog
		_fileChooser.AddButton("Cancel", ResponseType.Cancel);

		// Add an "Open" button to the dialog
		_fileChooser.AddButton("Open", ResponseType.Ok);

		// Set the initial directory (optional)
		_fileChooser.SetCurrentFolder("/");

		// Connect the response signal
		_fileChooser.Response += OnFileChooserResponse;

		// Show the dialog
		_fileChooser.Show();
		Application.Run();
	}

	private void OnFileChooserResponse(object sender, ResponseArgs args)
	{
		if (args.ResponseId == ResponseType.Ok)
		{
			// The user selected a file
			_settings.SaveDirectory = _fileChooser.File.Path;
			_locationButton.Text = _settings.SaveDirectory;
		}

		// Clean up resources4
		_saveManager._settings = _settings;
		_saveManager.WriteSave();
		_fileChooser.Dispose();
		Application.Quit();
	}

	private void ErrorPopup(String error)
	{
		_errorLabel.Text = "Error:" + error;
		_errorPopup.Popup();
	}
}

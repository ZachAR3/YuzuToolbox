using Godot;
using System;
using Mono.Posix;
using System.IO;
using System.Net.Mime;
using System.Security.AccessControl;
using System.Text;
using Gdk;
using Godot.Collections;
using Gtk;
using Mono.Unix;
using FileAccess = Godot.FileAccess;
using ProgressBar = Godot.ProgressBar;
using Window = Godot.Window;


public partial class Home : Control
{
	[Export()] private float _appVersion = 1f;
	
	[Export()] private OptionButton _versionButton;
	[Export()] private Godot.Button _locationButton;
	[Export()] private Godot.Button _downloadButton;
	[Export()] private Godot.CheckBox _autoExtractButton;
	[Export()] private ProgressBar _downloadProgressBar;
	[Export()] private Godot.CheckBox _customVersionCheckBox;
	[Export()] private SpinBox _customVersionSpinBox;
	[Export()] private Timer _downloadUpdateTimer;
	[Export()] private Popup _errorPopup;
	[Export()] private Godot.Label _errorLabel;
	[Export()] private HttpRequest _latestReleaseRequester;
	[Export()] private HttpRequest _downloadRequester;
	[Export()] private String _pineappleLatestUrl;
	[Export()] private String _pineappleDownloadBaseUrl;
	[Export()] private String _windowsFolderName = "yuzu-windows-msvc-early-access";
	[Export()] private string _yuzuBaseString = "Yuzu-EA-";
	[Export()] private string _saveName;
	[Export()] private int _previousVersionsToAdd = 10;
	[Export()] private Array<Theme> _themes;

	private FileChooserDialog _fileChooser;
	private ResourceSaveManager _saveManager;
	private SettingsResource _settings;
	private String _osUsed;
	private string _yuzuExtensionString;
	private Theme _currentTheme;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_currentTheme = _themes[0];
		_osUsed = OS.GetName();
		if (_osUsed == "Linux")
		{
			_saveName += ".AppImage";
			_yuzuExtensionString = ".AppImage";
			_autoExtractButton.Disabled = true;
		}
		else
		{
			_saveName += ".zip";
			_yuzuExtensionString = ".zip";
		}
		
		_saveManager = new ResourceSaveManager();
		_saveManager.Version = _appVersion;
		GetSettings();
		_locationButton.Text = _settings.SaveDirectory;
		
		// Call a request to get the latest versions and connect it to our GetNewVersions function
		_latestReleaseRequester.RequestCompleted += AddVersions;
		_latestReleaseRequester.Request(_pineappleLatestUrl);

		_downloadButton.Disabled = true;
		_downloadButton.Pressed += InstallSelectedVersion;
		_locationButton.Pressed += OpenFileChooser;
		_downloadRequester.RequestCompleted += VersionDownloadCompleted;
		_downloadUpdateTimer.Timeout += UpdateDownloadBar;
		
		Resized += WindowResized;

		_customVersionCheckBox.Toggled += CustomVersionSpinBoxEditable;
		_customVersionSpinBox.Editable = false;
	}


	private void WindowResized()
	{
		float scaleRatio = (float)GetWindow().Size.X / 1920;
		_currentTheme.DefaultFontSize = Mathf.Clamp((int)(scaleRatio * 35), 20, 50);
	}


	private void CustomVersionSpinBoxEditable(bool editable)
	{
		_customVersionSpinBox.Editable = editable;
		_versionButton.Disabled = editable;
	}
	
	
	private void InstallSelectedVersion()
	{
		int version;
		DeleteOldVersion();
		
		// Set old install (if it exists) to not be disabled anymore.
		if (_settings.InstalledVersion != -1)
		{
			_versionButton.SetItemDisabled(_versionButton.GetItemIndex(_settings.InstalledVersion), false);
		}

		if (_customVersionCheckBox.ButtonPressed)
		{
			version = (int)_customVersionSpinBox.Value;
		}
		else
		{
			int versionIndex = _versionButton.Selected;
			version = _versionButton.GetItemText(versionIndex).ToInt();
		}

		_downloadButton.Disabled = true;
		_locationButton.Disabled = true;
		_settings.InstalledVersion = version;
		_downloadButton.Text = "Downloading...";
		_downloadRequester.DownloadFile = $@"{_settings.SaveDirectory}/{_saveName}";
		_downloadRequester.Request($@"{_pineappleDownloadBaseUrl}{version}/{_osUsed}-{_yuzuBaseString}{version}{_yuzuExtensionString}");
		_downloadUpdateTimer.Start();
	}


	private void VersionDownloadCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_downloadUpdateTimer.Stop();
		_downloadButton.Disabled = false;
		_locationButton.Disabled = false;
		if (result == (int)HttpRequest.Result.Success)
		{
			_saveManager._settings = _settings;
			_saveManager.WriteSave();
			_downloadProgressBar.Value = 100;
			_downloadButton.Text = "Successfully Downloaded!";
			
			AddInstalledVersion();
			UnpackAndSetPermissions();
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
			_customVersionSpinBox.Value = latestVersion;

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

			_downloadButton.Disabled = false;
		}
		else
		{
			CallDeferred("ErrorPopup", "Failed to get latest versions error code: " + responseCode);
		}
	}


	private void AddInstalledVersion()
	{
		var installedVersion = _settings.InstalledVersion;
		var selectedIndex = _versionButton.GetItemIndex(installedVersion);
		_customVersionSpinBox.Value = installedVersion;
				
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
		string searchName = $@"{_osUsed}-{_yuzuBaseString}";
		int versionIndex = rawVersionData.Find(searchName);
		//GD.Print(versionIndex);

		// Using our starting index subtract the index of our extension from it and add 1 to get the length of the version
		int versionLength =  rawVersionData.Find(_yuzuExtensionString) -versionIndex -searchName.Length;
		//GD.Print(versionLength);
		
		// Return version by starting at our start index (accounting for our search string) and going the previously determined length
		return rawVersionData.Substring(versionIndex + searchName.Length, versionLength).ToInt();
	}


	private void UnpackAndSetPermissions()
	{
		string yuzuPath = GetExistingVersion();
		if (_osUsed == "Linux")
		{
			var yuzuFile = new Mono.Unix.UnixFileInfo(yuzuPath)
			{
				FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute
			};
		}
		else if (_osUsed == "Windows")
		{
			if (_autoExtractButton.ButtonPressed)
			{
				System.IO.Compression.ZipFile.ExtractToDirectory(yuzuPath, _settings.SaveDirectory);
				String yuzuWindowsDirectory = $@"{_settings.SaveDirectory}/{_windowsFolderName}";
				if (Directory.Exists(yuzuWindowsDirectory))
				{
					MoveFilesAndDirs(yuzuWindowsDirectory, _settings.SaveDirectory);
				}
			}
		}
	}

	
	private void GetSettings()
	{
		if (ResourceSaveManager.SaveExists())
		{
			var lastSave = (ResourceSaveManager)ResourceSaveManager.LoadSaveGame();
			
			if (lastSave.Version != _appVersion)
			{
				CallDeferred("ErrorPopup", $@"Error loading settings, version mismatch detected. Settings have been regenerated.");
				_saveManager._settings = new SettingsResource();
				_saveManager.WriteSave();
			}
			_settings = lastSave._settings;

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
				if (file.GetExtension() == "AppImage" || file.GetExtension() == "zip")
				{
					return $@"{_settings.SaveDirectory}/{file}";
				}
			}
		}	

		return "";
	}


	private void DeleteOldVersion()
	{
		var oldVersion = GetExistingVersion();
		
		if (_osUsed == "Linux")
		{
			if (oldVersion != "")
			{
				File.Delete(oldVersion);
			}
		}
		
		else if (_osUsed == "Windows")
		{
			if (_autoExtractButton.ButtonPressed)
			{
				DeleteDirectoryContents(_settings.SaveDirectory);
			}
			else
			{
				if (oldVersion != "")
				{
					File.Delete(oldVersion);
				}
			}
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


	static void MoveFilesAndDirs(string sourceDirectory, string targetDirectory)
	{
		// Create the target directory if it doesn't exist
		if (!Directory.Exists(targetDirectory))
		{
			Directory.CreateDirectory(targetDirectory);
		}

		// Get all files and directories from the source directory
		string[] files = Directory.GetFiles(sourceDirectory);
		string[] directories = Directory.GetDirectories(sourceDirectory);

		// Move files to the target directory
		foreach (string file in files)
		{
			string fileName = Path.GetFileName(file);
			string targetPath = Path.Combine(targetDirectory, fileName);
			File.Move(file, targetPath);
		}

		// Move directories to the target directory
		foreach (string directory in directories)
		{
			string directoryName = Path.GetFileName(directory);
			string targetPath = Path.Combine(targetDirectory, directoryName);
			Directory.Move(directory, targetPath);
		}

		// Optional: Remove the source directory if it is empty
		if (Directory.GetFiles(sourceDirectory).Length == 0 && Directory.GetDirectories(sourceDirectory).Length == 0)
		{
			Directory.Delete(sourceDirectory);
		}
	}
	
	
	
	static void DeleteDirectoryContents(string directoryPath)
	{
		// Delete all files within the directory
		string[] files = Directory.GetFiles(directoryPath);
		foreach (string file in files)
		{
			File.Delete(file);
		}

		// Delete all subdirectories within the directory
		string[] directories = Directory.GetDirectories(directoryPath);
		foreach (string directory in directories)
		{
			DeleteDirectoryContents(directory); // Recursively delete subdirectory contents
			Directory.Delete(directory);
		}
	}
	

	private void ErrorPopup(String error)
	{
		_errorLabel.Text = $@"Error:{error}";
		_errorPopup.Visible = true;
		_errorPopup.InitialPosition = Window.WindowInitialPosition.Absolute;
		_errorPopup.PopupCentered();
	}
}

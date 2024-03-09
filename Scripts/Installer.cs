using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Unix;
using NativeFileDialogSharp;
using Octokit;
using WindowsShortcutFactory;
using Label = Godot.Label;

public partial class Installer : Control
{
	// Exported variables (Primarily for the UI / Interactions)
	[ExportGroup("General")]
	[Export] private Label _latestVersionLabel;

	[ExportGroup("Installer")]
	[Export] private string _titlesKeySite;
	[Export] private int _previousVersionsToAdd = 10;
	[Export] private int _versionsPerPage = 10;
	[Export] private Image _icon;
	[Export] private OptionButton _versionButton;
	[Export] private CheckBox _createShortcutButton;
	[Export] private CheckBox _autoUpdateButton;
	[Export] private LineEdit _executableNameLineEdit;
	[Export] private Button _installLocationButton;
	[Export] private Button _downloadButton;
	[Export] private Panel _downloadWindow;
	[Export] private Label _downloadLabel;
	[Export] private Timer _downloadUpdateTimer;
	[Export] private ProgressBar _downloadProgressBar;
	[Export] private CheckBox _clearShadersButton;
	[Export] private Button _shadersLocationButton;
	[Export] private CheckBox _autoUnpackButton;
	[Export] private CheckBox _customVersionCheckBox;
	[Export] private SpinBox _customVersionSpinBox;
	[Export] private HttpRequest _downloadRequester;
	[Export] private TextureRect _extractWarning;
	[Export] private TextureRect _downloadWarning;
	[Export] private TextureRect _clearShadersWarning;

	// Internal variables
	private String _osUsed = OS.GetName();
	private string _yuzuExtensionString;
	private string _yuzuExecutableName;
	private string _executableSaveName;
	private int _latestRelease;
	private bool _autoUpdate;


	// Godot functions
	private void Initiate()
	{
		if (_osUsed == "Linux")
		{
			_executableSaveName += ".AppImage";
			_yuzuExtensionString = ".AppImage";
			_autoUnpackButton.Disabled = true;
		}
		else if (_osUsed == "Windows")
		{
			_executableSaveName += ".zip";
			_yuzuExtensionString = ".zip";
			_createShortcutButton.Disabled = true;
		}
		
		_yuzuExecutableName = Globals.Instance.Settings.ExecutableName;
		_executableNameLineEdit.Text = _yuzuExecutableName;
		_shadersLocationButton.Text = Globals.Instance.Settings.ShadersLocation;
		_installLocationButton.Text = Globals.Instance.Settings.SaveDirectory;
		_downloadButton.Disabled = true;
		_downloadWindow.Visible = false;
		_customVersionSpinBox.Editable = false;
		_extractWarning.Visible = false;
		_downloadWarning.Visible = false;
		_clearShadersWarning.Visible = false;
		
		AddVersions();
	}



	// Custom functions
	private async void InstallSelectedVersion()
	{
		// Launches confirmation window, and cancels if not confirmed.
		var confirm = await Tools.Instance.ConfirmationPopup();
		if (confirm != true)
		{
			return;
		}

		int selectedVersion;

		if (_customVersionCheckBox.ButtonPressed)
		{
			selectedVersion = (int)_customVersionSpinBox.Value;
		}
		else
		{
			int versionIndex = _versionButton.Selected;
			selectedVersion = _versionButton.GetItemText(versionIndex).ToInt();
		}
		InstallVersion(selectedVersion);
	}



	private void InstallVersion(int version)
	{
		DeleteOldVersion();
		
		// Set old install (if it exists) to not be disabled anymore.
		if (Globals.Instance.Settings.InstalledVersion != -1)
		{
			_versionButton.SetItemDisabled(_versionButton.GetItemIndex(Globals.Instance.Settings.InstalledVersion), false);
		}
		
		_executableSaveName = _executableSaveName.Insert(0, _yuzuExecutableName);
		_customVersionCheckBox.Disabled = true;
		_versionButton.Disabled = true;
		_downloadButton.Disabled = true;
		_installLocationButton.Disabled = true;
		Globals.Instance.Settings.InstalledVersion = version;
		_downloadLabel.Text = "Downloading...";
		_downloadWindow.Visible = true;
		_downloadLabel.GrabFocus();
		_downloadRequester.DownloadFile = $@"{Globals.Instance.Settings.SaveDirectory}/{_executableSaveName}";
		_downloadRequester.Request(Globals.Instance.Settings.AppMode.GetDownloadLink(version, _osUsed));
		_downloadUpdateTimer.Start();
		_downloadLabel.Text = "Downloading...";
	}
	
	

	private void UpdateDownloadBar()
	{
		_downloadProgressBar.Value =
			(float)_downloadRequester.GetDownloadedBytes() / _downloadRequester.GetBodySize() * 100;
	}


	private void CreateShortcut()
	{
		String linuxShortcutName = "yuzu-ea.desktop";
		String windowsShortcutName = "yuzu-ea.lnk";
		String iconPath = Path.Join(Globals.Instance.Settings.SaveDirectory, "Icon.png");

		string executable = _autoUpdate ? OS.GetExecutablePath() : Globals.Instance.Settings.ExecutablePath;
		string launcherFlag = null;
		if (_autoUpdate)
		{
			launcherFlag = "--launcher";
		}
		else
		{
			GetExistingVersion();
		}

		if (!File.Exists(executable))
		{
			Tools.Instance.AddError("No executable path found, shortcut creation failed... Please contact a developer...");
			return;
		}
		
		if (_osUsed == "Linux")
		{
			_icon.SavePng(iconPath);
			string shortcutContent = $@"
[Desktop Entry]
Comment=Nintendo Switch video game console emulator
Exec={executable} {launcherFlag}
GenericName=Switch Emulator
Icon={iconPath}
MimeType=
Name=Yuzu-EA
Path=
StartupNotify=true
Terminal=false
TerminalOptions=
Type=Application
Keywords=Nintendo;Switch;
Categories=Game;Emulator;Qt;
";

			if (Directory.Exists("/usr/share/applications/"))
			{
				string shortcutPath = $@"/usr/share/applications/{linuxShortcutName}";

				try
				{
					string tempShortcutPath = $@"{Globals.Instance.Settings.SaveDirectory}/{linuxShortcutName}";
					File.WriteAllText(tempShortcutPath, shortcutContent);
					ProcessStartInfo startInfo = new ProcessStartInfo
					{
						FileName = "pkexec",
						Arguments = $"mv {tempShortcutPath} {shortcutPath}",
						UseShellExecute = false
					};

					Process process = new Process { StartInfo = startInfo };
					process.Start();
					process.WaitForExit();
				}
				catch (Exception shortcutError)
				{
					shortcutPath = $@"{Globals.Instance.Settings.SaveDirectory}/{linuxShortcutName}";
					Tools.Instance.AddError(
						$@"Error creating shortcut, creating new at {shortcutPath}. Error:{shortcutError}");
					File.WriteAllText(shortcutPath, shortcutContent);
				}
			}
			else
			{
				Tools.Instance.AddError("Cannot find shortcut directory, please place manually.");
			}
		}
		else if (_osUsed == "Windows")
		{
			string commonStartMenuPath =
				System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu);
			string yuzuStartMenuPath = Path.Combine(commonStartMenuPath, "Programs", "yuzu-ea");
			string yuzuShortcutPath = Path.Combine(yuzuStartMenuPath, windowsShortcutName);
			var windowsShortcut = new WindowsShortcut
			{
				Path = executable,
				IconLocation = Globals.Instance.Settings.ExecutablePath,
				Arguments = launcherFlag
			};


			try
			{
				if (!Directory.Exists(yuzuStartMenuPath))
				{
					Directory.CreateDirectory(yuzuStartMenuPath);
				}

				windowsShortcut.Save(yuzuShortcutPath);
			}
			catch (Exception shortcutError)
			{
				yuzuShortcutPath = $@"{Globals.Instance.Settings.SaveDirectory}/{windowsShortcutName}";
				Tools.Instance.AddError(
					$@"cannot create shortcut, ensure app is running as admin. Placing instead at {yuzuShortcutPath}. Exception:{shortcutError}");
				windowsShortcut.Save(yuzuShortcutPath);
			}

		}
	}


	private async void AddVersions()
	{
		try
		{
			await GetLatestVersion();
			if (_latestRelease == -1)
			{
				Tools.Instance.AddError("Unable to fetch latest Pineapple release");
				return;
			}

			_customVersionSpinBox.Value = _latestRelease;
			_latestVersionLabel.Text = $"Latest: {_latestRelease.ToString()}";
			
			//Add a version item for the latest and the dictated amount of previous versions.
			for (int previousIndex = 0; previousIndex < _previousVersionsToAdd; previousIndex++)
			{
				_versionButton.AddItem((_latestRelease - previousIndex).ToString(), _latestRelease - previousIndex);
			}
			
			//Checks if there is already a version installed, and if so adds it.
			if (Globals.Instance.Settings.InstalledVersion != -1)
			{
				AddInstalledVersion();
			}
			
			_downloadButton.Disabled = false;
			
			// If running in launcher mode updates and launches yuzu
			if (Globals.Instance.Settings.LauncherMode)
			{
				if (_latestRelease != Globals.Instance.Settings.InstalledVersion)
				{
					// Yuzu will be launched inside of the download function when the download is completed.
					InstallVersion(_latestRelease);
				}
				else
				{
					Tools.Instance.LaunchYuzu();
				}
			}
		}
		catch (Exception versionPullException)
		{
			Tools.Instance.AddError("Failed to get latest versions error code: " + versionPullException);
		}
	}


	private void AddInstalledVersion()
	{
		var installedVersion = Globals.Instance.Settings.InstalledVersion;
		var selectedIndex = _versionButton.GetItemIndex(installedVersion);
		_customVersionSpinBox.Value = installedVersion;

		// Checks if the item was already added, if so sets it as current, otherwise adds a new item entry for it.
		if (selectedIndex >= 0)
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


	private async Task GetLatestVersion()
	{
		// Trys to fetch version using github API if failed, tries to web-scrape it.
		try
		{
			var gitHubClient = Globals.Instance.LocalGithubClient;

			var latestRelease =
				await gitHubClient.Repository.Release.GetLatest(_repoOwner, _repoName);
			_latestRelease = latestRelease.TagName.Split("-").Last().ToInt();
		}
		// Fall back version grabber
		catch (RateLimitExceededException)
		{
			Tools.Instance.AddError("Github API rate limit exceeded, falling back to web-scraper. Some sources may not function until requests have reset");
			
			var httpClient = new System.Net.Http.HttpClient();
			var rawVersionData = httpClient.GetAsync(_pineappleLatestUrl).Result.Content.ReadAsStringAsync().Result;
			
			_latestRelease = rawVersionData.Split("EA-").Last().Split("\"").First().ToInt();
		}
		
	}


	private void UnpackAndSetPermissions()
	{
		string yuzuPath = $@"{Globals.Instance.Settings.SaveDirectory}/{_executableSaveName}";
		if (_osUsed == "Linux")
		{
			var yuzuFile = new UnixFileInfo(yuzuPath)
			{
				FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute
			};
			Globals.Instance.Settings.ExecutablePath = yuzuPath;
		}
		else if (_osUsed == "Windows")
		{
			if (_autoUnpackButton.ButtonPressed || _autoUpdate)
			{
				System.IO.Compression.ZipFile.ExtractToDirectory(yuzuPath, Globals.Instance.Settings.SaveDirectory);
				String yuzuWindowsDirectory = $@"{Globals.Instance.Settings.SaveDirectory}/{_windowsFolderName}";
				if (Directory.Exists(yuzuWindowsDirectory))
				{
					// Moves the files from the temp folder into the save directory
					Tools.MoveFilesAndDirs(yuzuWindowsDirectory, Globals.Instance.Settings.SaveDirectory);
					// Creates the executable path to yuzu.exe (hardcoded, but due to the prevalence of .exe's in the folder no better ways to do it)
					var currentExecutablePath = Path.Join(Globals.Instance.Settings.SaveDirectory, "yuzu.exe");
					var newExecutablePath = Path.Join(Globals.Instance.Settings.SaveDirectory,
						$"{_yuzuExecutableName}.exe");
					// Essentially renames the .exe into the yuzu executable name
					if (currentExecutablePath != newExecutablePath)
					{
						File.Move(currentExecutablePath, newExecutablePath);
					}
					Globals.Instance.Settings.ExecutablePath = newExecutablePath;
				}
			}
		}
	}


	private String GetExistingVersion()
	{
		if (DirAccess.DirExistsAbsolute(Globals.Instance.Settings.SaveDirectory))
		{
			var previousSave = DirAccess.Open(Globals.Instance.Settings.SaveDirectory);

			foreach (var file in previousSave.GetFiles())
			{
				if (file.GetExtension() == "AppImage" || file.GetBaseName() == _executableSaveName)
				{
					return $@"{Globals.Instance.Settings.SaveDirectory}/{file}";
				}
			}
		}

		Tools.Instance.AddError("Unable to find existing version");
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
			if (_autoUnpackButton.ButtonPressed)
			{
				Tools.DeleteDirectoryContents(Globals.Instance.Settings.SaveDirectory);
			}
			else
			{
				if (oldVersion != "")
				{
					File.Delete(oldVersion);
				}
			}
		}

		if (_clearShadersButton.ButtonPressed)
		{
			Tools.Instance.ClearShaders(Globals.Instance.Settings.ShadersLocation);
			;
		}
	}


	// Signal functions
	private  void OnShadersLocationButtonPressed()
	{
		var shadersLocationInput = Dialog.FolderPicker(Globals.Instance.Settings.ShadersLocation).Path;
		if (shadersLocationInput != null)
		{
			Globals.Instance.Settings.ShadersLocation = shadersLocationInput;
		}

		_shadersLocationButton.Text = Globals.Instance.Settings.ShadersLocation;	
		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
	}
	
	
	private void OnInstallLocationButtonPressed()
	{
		var saveDirectoryLocationInput = Dialog.FolderPicker(Globals.Instance.Settings.SaveDirectory).Path;
		if (saveDirectoryLocationInput != null)
		{
			Globals.Instance.Settings.SaveDirectory = saveDirectoryLocationInput;
		}
		
		_installLocationButton.Text = Globals.Instance.Settings.SaveDirectory;
		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
	}


	private void AutoUnpackToggled(bool unpackEnabled)
	{
		// If unpack is toggled off, ensures the create shortcut button is also disabled and turns off.
		_createShortcutButton.ButtonPressed = unpackEnabled && _createShortcutButton.ButtonPressed;
		_createShortcutButton.Disabled = !unpackEnabled;
		_downloadWarning.Visible = _extractWarning.Visible || unpackEnabled;
		_extractWarning.Visible = unpackEnabled;
	}

	private void ClearShadersToggle(bool clearEnabled)
	{
		_clearShadersWarning.Visible = clearEnabled;
		_downloadWarning.Visible = _extractWarning.Visible || clearEnabled;
	}
	
	
	private void VersionDownloadCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_downloadUpdateTimer.Stop();
		_customVersionCheckBox.Disabled = false;
		_downloadButton.Disabled = false;
		_installLocationButton.Disabled = false;
		_versionButton.Disabled = false;
		if (result == (int)HttpRequest.Result.Success)
		{
			// Used to save version installed after download.
			Globals.Instance.SaveManager.WriteSave();
			_downloadProgressBar.Value = 100;
			_downloadLabel.Text = "Successfully Downloaded!";

			AddInstalledVersion();
			UnpackAndSetPermissions();
			if (_createShortcutButton.ButtonPressed)
			{
				_downloadWindow.Visible = false;
				CreateShortcut();
			}

			_downloadWindow.Visible = false;

			if (Globals.Instance.Settings.LauncherMode)
			{
				Tools.Instance.LaunchYuzu();
			}
			
			Globals.Instance.SaveManager.WriteSave();
		}
		else
		{
			Tools.Instance.AddError("Failed to download, error:" + result);
			_downloadProgressBar.Value = 0;
		}
	}
	
	
	private void CustomVersionSpinBoxEditable(bool editable)
	{
		_customVersionSpinBox.Editable = editable;
		_versionButton.Disabled = editable;
	}


	private void ExecutableNameChanged(string newName)
	{
		_yuzuExecutableName = newName;
		Globals.Instance.Settings.ExecutableName = _yuzuExecutableName;
		Globals.Instance.SaveManager.WriteSave();
	}


	private void AutoUpdateToggled(bool autoUpdate)
	{
		if (_createShortcutButton.ButtonPressed || !autoUpdate)
		{
			_autoUpdate = autoUpdate;
		}
		_autoUpdateButton.ButtonPressed = _autoUpdate;
	}
}

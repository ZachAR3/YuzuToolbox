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
	[Export()] private Label _latestVersionLabel;

	[ExportGroup("Installer")] 
	[Export()] private string _repoName;
	[Export()] private string _repoOwner;
	[Export()] private string _pineappleLatestUrl;
	[Export()] private string _pineappleDownloadBaseUrl;
	[Export()] private string _titlesKeySite;
	[Export()] private string _windowsFolderName = "yuzu-windows-msvc-early-access";
	[Export()] private string _yuzuBaseString = "Yuzu-EA-";
	[Export()] private string _saveName;
	[Export()] private int _previousVersionsToAdd = 10;
	[Export()] private int _versionsPerPage = 10;
	[Export()] private Image _icon;
	[Export()] private OptionButton _versionButton;
	[Export()] private CheckBox _createShortcutButton;
	[Export()] private Button _installLocationButton;
	[Export()] private Button _downloadButton;
	[Export()] private Panel _downloadWindow;
	[Export()] private Label _downloadLabel;
	[Export()] private Timer _downloadUpdateTimer;
	[Export()] private ProgressBar _downloadProgressBar;
	[Export()] private CheckBox _clearShadersButton;
	[Export()] private Button _shadersLocationButton;
	[Export()] private CheckBox _autoUnpackButton;
	[Export()] private CheckBox _customVersionCheckBox;
	[Export()] private SpinBox _customVersionSpinBox;
	[Export()] private HttpRequest _downloadRequester;
	[Export()] private TextureRect _extractWarning;
	[Export()] private TextureRect _downloadWarning;
	[Export()] private TextureRect _clearShadersWarning;

	// Internal variables
	private String _osUsed = OS.GetName();
	private string _yuzuExtensionString;
	

	// Godot functions
	private void Initiate()
	{
		if (_osUsed == "Linux")
		{
			_saveName += ".AppImage";
			_yuzuExtensionString = ".AppImage";
			_autoUnpackButton.Disabled = true;
		}
		else if (_osUsed == "Windows")
		{
			_saveName += ".zip";
			_yuzuExtensionString = ".zip";
			_createShortcutButton.Disabled = true;
		}

		_shadersLocationButton.Text = Globals.Instance.Settings.ShadersLocation;
		_installLocationButton.Text = Globals.Instance.Settings.SaveDirectory;
		_downloadButton.Disabled = true;
		_downloadWindow.Visible = false;
		_customVersionSpinBox.Editable = false;
		_extractWarning.Visible = false;
		_downloadWarning.Visible = false;
		_clearShadersWarning.Visible = false;
		
		_downloadButton.GrabFocus();
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

		int version;
		DeleteOldVersion();

		// Set old install (if it exists) to not be disabled anymore.
		if (Globals.Instance.Settings.InstalledVersion != -1)
		{
			_versionButton.SetItemDisabled(_versionButton.GetItemIndex(Globals.Instance.Settings.InstalledVersion), false);
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

		_customVersionCheckBox.Disabled = true;
		_versionButton.Disabled = true;
		_downloadButton.Disabled = true;
		_installLocationButton.Disabled = true;
		Globals.Instance.Settings.InstalledVersion = version;
		_downloadLabel.Text = "Downloading...";
		_downloadWindow.Visible = true;
		_downloadLabel.GrabFocus();
		_downloadRequester.DownloadFile = $@"{Globals.Instance.Settings.SaveDirectory}/{_saveName}";
		_downloadRequester.Request(
			$@"{_pineappleDownloadBaseUrl}{version}/{_osUsed}-{_yuzuBaseString}{version}{_yuzuExtensionString}");
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
		String iconPath = $@"{Globals.Instance.Settings.SaveDirectory}/Icon.png";

		if (_osUsed == "Linux")
		{
			_icon.SavePng(iconPath);
			string shortcutContent = $@"
[Desktop Entry]
Comment=Nintendo Switch video game console emulator
Exec={GetExistingVersion()}
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
					throw;
				}
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
				Path = GetExistingVersion()
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
				throw;
			}

		}
	}


	private async void AddVersions()
	{
		try
		{
			var latestVersion = await GetLatestVersion();
			if (latestVersion == -1)
			{
				return;
			}

			_customVersionSpinBox.Value = latestVersion;
			_latestVersionLabel.Text = $"Latest: {latestVersion.ToString()}";
			
			//Add a version item for the latest and the dictated amount of previous versions.
			for (int previousIndex = 0; previousIndex < _previousVersionsToAdd; previousIndex++)
			{
				_versionButton.AddItem((latestVersion - previousIndex).ToString(), latestVersion - previousIndex);
			}
			
			//Checks if there is already a version installed, and if so adds it.
			if (Globals.Instance.Settings.InstalledVersion != -1)
			{
				AddInstalledVersion();
			}
			
			_downloadButton.Disabled = false;
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


	private async Task<int> GetLatestVersion()
	{
		// Trys to fetch version using github API if failed, tries to web-scrape it.
		try
		{
			var gitHubClient = Globals.Instance.LocalGithubClient;

			var latestRelease =
				await gitHubClient.Repository.Release.GetLatest(_repoOwner, _repoName);
			return latestRelease.TagName.Split("-").Last().ToInt();
		}
		// Fall back version grabber
		catch (RateLimitExceededException)
		{
			Tools.Instance.AddError("Github API rate limit exceeded, falling back to web-scraper. Some sources may not function until requests have reset");
			
			var httpClient = new System.Net.Http.HttpClient();
			var rawVersionData = httpClient.GetAsync(_pineappleLatestUrl).Result.Content.ReadAsStringAsync().Result;
			
			return (rawVersionData.Split("EA-").Last()).Split("\"").First().ToInt();
		}
		
	}


	private void UnpackAndSetPermissions()
	{
		string yuzuPath = $@"{Globals.Instance.Settings.SaveDirectory}/{_saveName}";
		if (_osUsed == "Linux")
		{
			var yuzuFile = new UnixFileInfo(yuzuPath)
			{
				FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute
			};
		}
		else if (_osUsed == "Windows")
		{
			if (_autoUnpackButton.ButtonPressed)
			{
				System.IO.Compression.ZipFile.ExtractToDirectory(yuzuPath, Globals.Instance.Settings.SaveDirectory);
				String yuzuWindowsDirectory = $@"{Globals.Instance.Settings.SaveDirectory}/{_windowsFolderName}";
				if (Directory.Exists(yuzuWindowsDirectory))
				{
					Tools.MoveFilesAndDirs(yuzuWindowsDirectory, Globals.Instance.Settings.SaveDirectory);
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
				if (file.GetExtension() == "AppImage" || file.GetBaseName() == "yuzu")
				{
					return $@"{Globals.Instance.Settings.SaveDirectory}/{file}";
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
			Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
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
		}
		else
		{
			_downloadProgressBar.Value = 0;
		}
	}
	
	
	private void CustomVersionSpinBoxEditable(bool editable)
	{
		_customVersionSpinBox.Editable = editable;
		_versionButton.Disabled = editable;
	}
}

using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Unix;
using NativeFileDialogSharp;
using Octokit;
using WindowsShortcutFactory;
using YuzuToolbox.Scripts.Modes;
using Label = Godot.Label;
using SharpCompress.Common;
using SharpCompress.Readers;


public partial class Installer : Control
{
	// Exported variables (Primarily for the UI / Interactions)
	[ExportGroup("General")]
	[Export] private Label _latestVersionLabel;

	[ExportGroup("Installer")]
	[Export] private string _titlesKeySite;
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
	[Export] private LineEdit _customVersionLineEdit;
	[Export] private HttpRequest _downloadRequester;
	[Export] private TextureRect _extractWarning;
	[Export] private TextureRect _downloadWarning;
	[Export] private TextureRect _clearShadersWarning;

	// Internal variables
	private String _osUsed = OS.GetName();
	// private string _yuzuExtensionString;
	private string _executableName;
	private string _executableSaveName;
	private int _latestRelease;
	private bool _autoUpdate;
	private Mode AppMode => Globals.Instance.AppMode;
	private SettingsResource Settings => Globals.Instance.Settings;
	
	private readonly System.Net.Http.HttpClient _httpClient = new();


	// Godot functions
	private void Initiate()
	{
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "YuzuToolbox");
		_createShortcutButton.Disabled = _osUsed == "Windows";
		
		// TODO
		if (AppMode.Name == "Yuzu")
		{
			if (_osUsed == "Linux")
			{
				_executableSaveName = ".AppImage";
				_autoUnpackButton.Disabled = true;
			}
			else if (_osUsed == "Windows")
			{
				_executableSaveName = ".zip";
				_createShortcutButton.Disabled = true;
			}
		}
		else
		{
			_executableSaveName = ".tar.gz";
		}

		_executableName = AppMode.Name;
		_executableNameLineEdit.Text = _executableName;
		_shadersLocationButton.Text = Settings.ShadersLocation;
		_installLocationButton.Text = Settings.SaveDirectory;
		_downloadButton.Disabled = true;
		_downloadWindow.Visible = false;
		_customVersionLineEdit.Editable = false;
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
			if (Regex.IsMatch(_customVersionLineEdit.Text, @"[^0-9.]"))
			{
				Tools.Instance.AddError("Invalid version selected, please enter a valid version number.");
				return;
			}
			selectedVersion = Tools.ToInt(_customVersionLineEdit.Text.Trim());
		}
		else
		{
			int versionIndex = _versionButton.Selected;
			var version = _versionButton.GetItemText(versionIndex);
			selectedVersion = Tools.ToInt(version);
		}
		InstallVersion(selectedVersion);
	}



	private void InstallVersion(int version)
	{
		DeleteOldVersion();
		
		// Set old install (if it exists) to not be disabled anymore.
		if (Globals.Instance.Settings.InstalledVersion >= 0)
		{
			_versionButton.SetItemDisabled(_versionButton.GetItemIndex(Settings.InstalledVersion), false);
		}
		
		_executableSaveName = _executableSaveName.Insert(0, _executableName);
		_customVersionCheckBox.Disabled = true;
		_versionButton.Disabled = true;
		_downloadButton.Disabled = true;
		_installLocationButton.Disabled = true;
		Settings.InstalledVersion = version;
		_downloadLabel.Text = "Downloading...";
		_downloadWindow.Visible = true;
		_downloadLabel.GrabFocus();

		// Ensures save directory exists
		if (!Directory.Exists(Settings.SaveDirectory))
		{
			Directory.CreateDirectory(Settings.SaveDirectory);
		}
		
		_downloadRequester.DownloadFile = $@"{Settings.SaveDirectory}/{_executableSaveName}";
		_downloadRequester.Request(AppMode.GetDownloadLink(version, _osUsed));
		_downloadUpdateTimer.Start();
		_downloadLabel.Text = "Downloading...";
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

			if (Settings.LauncherMode)
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
	

	private void UpdateDownloadBar()
	{
		_downloadProgressBar.Value =
			(float)_downloadRequester.GetDownloadedBytes() / _downloadRequester.GetBodySize() * 100;
	}


	private void CreateShortcut()
	{
		String linuxShortcutName = "yuzu-ea.desktop";
		String windowsShortcutName = "yuzu-ea.lnk";
		String iconPath = Path.Join(Settings.SaveDirectory, "Icon.png");

		string executable = _autoUpdate ? OS.GetExecutablePath() : Settings.ExecutablePath;
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
					string tempShortcutPath = $@"{Settings.SaveDirectory}/{linuxShortcutName}";
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
					shortcutPath = $@"{Settings.SaveDirectory}/{linuxShortcutName}";
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
				IconLocation = Settings.ExecutablePath,
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
				yuzuShortcutPath = $@"{Settings.SaveDirectory}/{windowsShortcutName}";
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
				Tools.Instance.AddError("Unable to fetch latest emulator release");
				return;
			}

			// TODO Deprecate or fix custom versions
			//_customVersionSpinBox.Value = _latestRelease;
			
			_latestVersionLabel.Text = $"Latest: {(AppMode.Name == "Yuzu" ? _latestRelease : Tools.FromInt(_latestRelease))}";

			HttpResponseMessage releasesResponse = await _httpClient.GetAsync(AppMode.ReleasesUrl);
			var releasesContent = await releasesResponse.Content.ReadAsStringAsync();
			MatchCollection releasesMatches = Regex.Matches(releasesContent, @"<h2 class=""sr-only"".*?>(.*?)<\/h2>");
			foreach (Match match in releasesMatches)
			{
				string version = match.Groups[1].Value.Trim();
				_versionButton.AddItem(version, Tools.ToInt(version));
			}

		
			//Checks if there is already a version installed, and if so adds it.
			if (Settings.InstalledVersion >= 0)
			{
				AddInstalledVersion();
			}
			
			_downloadButton.Disabled = false;
			
			// If running in launcher mode updates and launches yuzu
			if (Settings.LauncherMode)
			{
				if (_latestRelease != Settings.InstalledVersion)
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
		var installedVersionInt = Settings.InstalledVersion;
		var installedVersion = AppMode.Name == "Yuzu" ? installedVersionInt.ToString() : Tools.FromInt(installedVersionInt);
		var selectedIndex = _versionButton.GetItemIndex(installedVersionInt);
		// Set the custom version to default of the currently installed one
		_customVersionLineEdit.Text = installedVersion;

		// Checks if the item was already added, if so sets it as current, otherwise adds a new item entry for it.
		if (selectedIndex >= 0)
		{
			_versionButton.Selected = selectedIndex;
		}
		else
		{
			_versionButton.AddItem(installedVersion, installedVersionInt);
			selectedIndex = _versionButton.GetItemIndex(installedVersionInt);
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
				await gitHubClient.Repository.Release.GetLatest(AppMode.RepoOwner, AppMode.RepoName);

			string release = latestRelease.TagName.Split("-").Last();
			_latestRelease = Tools.ToInt(release);
		}
		// TODO update for ryujinx
		// Fall back version grabber
		catch (RateLimitExceededException)
		{
			Tools.Instance.AddError("Github API rate limit exceeded, falling back to web-scraper. Some sources may not function until requests have reset");
			
			var rawVersionData = _httpClient.GetAsync(AppMode.LatestDownloadUrl).Result.Content.ReadAsStringAsync().Result;
			
			_latestRelease = rawVersionData.Split("EA-").Last().Split("\"").First().ToInt();
		}
		
	}


	private void UnpackAndSetPermissions()
	{
		string executablePath = $@"{Settings.SaveDirectory}/{_executableSaveName}";
		if (_osUsed == "Linux" && Settings.AppMode == "Yuzu")
		{
			var executableFile = new UnixFileInfo(executablePath)
			{
				FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute
			};
			Settings.ExecutablePath = executablePath;
		}
		else
		{
			if (_autoUnpackButton.ButtonPressed || _autoUpdate)
			{
				using Stream stream = File.OpenRead(executablePath);
				var reader = ReaderFactory.Open(stream);
				while (reader.MoveToNextEntry())
				{
					if (!reader.Entry.IsDirectory)
					{
						ExtractionOptions opt = new ExtractionOptions
						{
							ExtractFullPath = true,
							Overwrite = true
						};
						reader.WriteEntryToDirectory(Settings.SaveDirectory, opt);
					}
				}

				String yuzuWindowsDirectory = $@"{Settings.SaveDirectory}/{AppMode.WindowsFolderName}";
				if (Directory.Exists(yuzuWindowsDirectory))
				{
					// Moves the files from the temp folder into the save directory
					Tools.MoveFilesAndDirs(yuzuWindowsDirectory, Settings.SaveDirectory);
					// Creates the executable path to yuzu.exe (hardcoded, but due to the prevalence of .exe's in the folder no better ways to do it)
					var currentExecutablePath = Path.Join(Settings.SaveDirectory, "yuzu.exe");
					var newExecutablePath = Path.Join(Settings.SaveDirectory,
						$"{_executableName}.exe");
					// Essentially renames the .exe into the yuzu executable name
					if (currentExecutablePath != newExecutablePath)
					{
						File.Move(currentExecutablePath, newExecutablePath);
					}

					Settings.ExecutablePath = newExecutablePath;
				}
			}
		}
	}


	private String GetExistingVersion()
	{
		if (DirAccess.DirExistsAbsolute(Settings.SaveDirectory))
		{
			var previousSave = DirAccess.Open(Settings.SaveDirectory);

			foreach (var file in previousSave.GetFiles())
			{
				if (file.GetExtension() == "AppImage" || file.GetBaseName() == _executableSaveName)
				{
					return $@"{Settings.SaveDirectory}/{file}";
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
				Tools.DeleteDirectoryContents(Settings.SaveDirectory);
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
			Tools.Instance.ClearShaders(Settings.ShadersLocation);
			;
		}
	}


	// Signal functions
	private  void OnShadersLocationButtonPressed()
	{
		var shadersLocationInput = Dialog.FolderPicker(Settings.ShadersLocation).Path;
		if (shadersLocationInput != null)
		{
			Settings.ShadersLocation = shadersLocationInput;
		}

		_shadersLocationButton.Text = Settings.ShadersLocation;	
		Globals.Instance.SaveManager.WriteSave(Settings);
	}
	
	
	private void OnInstallLocationButtonPressed()
	{
		var saveDirectoryLocationInput = Dialog.FolderPicker(Settings.SaveDirectory).Path;
		if (saveDirectoryLocationInput != null)
		{
			Settings.SaveDirectory = saveDirectoryLocationInput;
		}
		
		_installLocationButton.Text = Settings.SaveDirectory;
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


	private void CustomVersionSpinBoxEditable(bool editable)
	{
		_customVersionLineEdit.Editable = editable;
		_versionButton.Disabled = editable;
	}


	private void ExecutableNameChanged(string newName)
	{
		_executableName = newName;
		// TODO check if necessary
		//AppMode.Name = newName;
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

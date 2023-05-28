using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot.Collections;
using HtmlAgilityPack;
using WindowsShortcutFactory;
using HttpClient = System.Net.Http.HttpClient;

public partial class ModManager : Control
{
	[ExportGroup("General")]
	[Export()] private Popup _errorPopup;
	[Export()] private Label _errorLabel;
	[Export()] private PopupMenu _confirmationPopup;
	
	[ExportGroup("ModManager")] 
	[Export()] private ItemList _modList;
	[Export()] private OptionButton _gamePickerButton;
	[Export()] private HttpRequest _titleRequester;
	[Export()] private Texture2D _installedIcon;

	private const string Quote = "\"";
	private string _currentGameId;
	private SettingsResource _settings;
	private ResourceSaveManager _saveManager = new ResourceSaveManager();
	private string _modsPath;
	private Tools _tools = new Tools();
	
	// Game id, game name
	private Godot.Collections.Dictionary<string, string> _titles = new Godot.Collections.Dictionary<string, string>();
	// Game id, mod names array
	private Godot.Collections.Dictionary<string, Game> _installedGames = new Godot.Collections.Dictionary<string, Game>();
	private Godot.Collections.Dictionary<string, Array<YuzuMod>> _yuzuModsList = new Godot.Collections.Dictionary<string, Array<YuzuMod>>();

	// Godot Functions
	public override void _Ready()
	{
		_settings = _saveManager.GetSettings();
		_titleRequester.Connect("request_completed", new Callable(this, nameof(GetTitles)));

		// Fix so doesn't overwrite previous saved locations TODO
		if (OS.GetName() == "Linux")
		{
			_settings.AppDataPath = $@"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)}/yuzu/";
		}
		else if (OS.GetName() == "Windows")
		{
			_settings.AppDataPath = $@"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData)}\yuzu\";
		}
		_modsPath = $@"{_settings.AppDataPath}load";
		GetGamesAndMods();
		AddMods(_currentGameId);
	}


	// Custom functions
	private async void GetGamesAndMods()
	{
		_titleRequester.Request(
			"https://switchbrew.org/w/index.php?title=Title_list/Games&mobileaction=toggle_view_desktop");
		await ToSignal(_titleRequester, "request_completed"); // Waits for titles to be retrieved before checking installed titles against them.
		
		foreach (var gameModFolder in Directory.GetDirectories(_modsPath))
		{
			string gameId = gameModFolder.GetFile(); // Gets game id by grabbing the folders name
			if (_titles.TryGetValue(gameId, out var gameName))
			{
				_installedGames[gameId] = new Game(gameName);
				if (!_yuzuModsList.ContainsKey(gameId))
				{
					_yuzuModsList[gameId] = new Array<YuzuMod>();
				}
				
				await GetAvailableMods(gameId, false);
				GetInstalledMods(gameId);
				_gamePickerButton.AddItem($@"    {gameName}");
			}
			else
			{
				// TODO better solution for informing user
				GD.Print("Cannot find title:" + gameId);
			}

		}
		// Sets the first game as selected by default
		SelectGame(0);
	}

	
	private async Task GetAvailableMods(string? gameId, bool useBananaMods)
	{
		// Used as a task so it is run on a separate thread and doesn't slow down app during startup
		await Task.Run(() =>
		{
			var htmlWeb = new HtmlWeb();
			if (gameId == null)
			{
				GD.PrintErr("No mod ID given, cannot add game mods.");
				return;
			}

			// Checks if there is a mod list for the given game, if not creates one
			if (!_yuzuModsList.ContainsKey(gameId))
			{
				_yuzuModsList[gameId] = new Array<YuzuMod>();
			}

			var modsSourcePage = htmlWeb.Load("https://github.com/yuzu-emu/yuzu/wiki/Switch-Mods");
			// List of elements, which MOSTLY contain mods (not all)
			var mods = modsSourcePage.DocumentNode.SelectNodes(
				$@"//h3[contains(., {Quote}{_installedGames[gameId].GameName}{Quote})]/following::table[1]//td//a");
			if (mods == null)
			{
				return;
			}

			int titleIndex = 1;
			for (int searchIndex = 0; searchIndex < mods.Count; searchIndex++)
			{
				var mod = mods[searchIndex];
				string downloadUrl = mod.GetAttributeValue("href", null).Trim();
				string modName = mod.InnerText;

				if (downloadUrl.EndsWith(".rar") || downloadUrl.EndsWith(".zip") || downloadUrl.EndsWith(".7z"))
				{
					var modVersions = modsSourcePage.DocumentNode.SelectNodes(
						$@"//h3[contains(., {Quote}{_installedGames[gameId].GameName}{Quote})]/following::table[1]//tr[{titleIndex}]/td//code");
					Array<string> versions = new Array<string>();
					foreach (var version in modVersions)
					{
						versions.Add(version.InnerText);
					}
					titleIndex++;

					_yuzuModsList[gameId].Add(new YuzuMod(modName, downloadUrl, versions, versions.Last(), false));
				}
			}
		});
		
	}


	private void GetInstalledMods(string gameId)
	{
		foreach (var modDirectory in Directory.GetDirectories($@"{_modsPath}/{gameId}"))
		{
			string[] modInfo = modDirectory.GetFile().Split("|");
			string modName = modInfo[0];
			string modVersion = modInfo.Length > 1 ? modInfo[1] : "N/A";
			bool available = false;
			foreach (var availableMod in _yuzuModsList[gameId])
			{
				if (availableMod.ModName.Contains(modName))
				{
					availableMod.IsInstalled = true;
					availableMod.CurrentVersion = modVersion;
					available = true;
					break;
				}
			}

			if (!available)
			{
				var versions = new Array<string>();
				versions.Add("N/A");
				_yuzuModsList[gameId].Add(new YuzuMod( modName, null, versions, modVersion, true));
			}
		}
	}


	// Adds available and local mods to mod list
	private void AddMods(string gameId)
	{
		// foreach (var gameId in _installedGames.Keys)
		// {
		if (!_yuzuModsList.ContainsKey(gameId))
		{
			return;
		}
		
		foreach (var mod in _yuzuModsList[gameId])
		{
			if (mod.IsInstalled)
			{
				_modList.AddItem($@"  {mod.ModName} - Supports:{string.Join(", ", mod.CompatibleVersions)}  ", icon: _installedIcon);
			}
			else
			{
				_modList.AddItem($@"  {mod.ModName} - Supports:{string.Join(", ", mod.CompatibleVersions)}  ");
			}
		}
		
	}


	private void GetTitles(long result, long responseCode, string[] headers, byte[] body)
	{
		string[] gamesArray = Encoding.UTF8.GetString(body).Split("<tr>"); // Splits the list into the begginings of each game
		var gameList = gamesArray.ToList(); // Converted to list so first and second item (headers and example text at top) can be removed
		gameList.RemoveRange(0, 2);

		foreach (string game in gameList)
		{
			// Removes the <td> and </td> html from our script for cleaning along with the special TM character otherwise the mod sites won't recognize the title.
			var gameCleaned = game.Replace("<td>", "").Replace("</td>", "").Replace("™", "");
			// Splits at every new line
			var gameSplit = gameCleaned.Split(System.Environment.NewLine);
			// Adds the game to our title list with type Dictionary(string ID, string Title, string modID)
			_titles[gameSplit[1]] = gameSplit[2];
		}
	}

	
	private async void UpdateAll()
	{
		foreach (var installedGame in _installedGames)
		{
			foreach (var mod in _yuzuModsList[installedGame.Key])
			{
				if (mod.IsInstalled && mod.ModUrl != null)
				{
					await Task.Run(async () =>
					{
						var modUpdated = await UpdateMod(installedGame.Key, mod.ModName, mod.ModUrl,
							mod.CompatibleVersions, mod.CurrentVersion);
						if (modUpdated != true)
						{
							return;
						}

						_modList.AddItem($@"  {mod.ModName} - Supports:{mod.CompatibleVersions}  ",
							icon: _installedIcon);
						// Used to update UI with installed icon
						SelectGame(_gamePickerButton.Selected);
					});
				}
			}
		}
	}
	

	private async Task<bool> InstallMod(string gameId, string modName, string modUrl, Array<string> compatibleVersions)
	{
		try
		{
			await Task.Run(async () =>
			{
				// Downloads mod zip
				string downloadPath = $@"{_modsPath}/{gameId}/{modName}-Download";
				string installPath = $@"{_modsPath}/{gameId}/{modName}|{compatibleVersions.Last()}";
				HttpClient httpClient = new HttpClient();
				byte[] downloadData = await httpClient.GetAsync(modUrl).Result.Content.ReadAsByteArrayAsync();
				// Should really make it so / is different for windows and linux TODO
				await File.WriteAllBytesAsync(downloadPath, downloadData);
				System.IO.Compression.ZipFile.ExtractToDirectory(downloadPath, installPath + "-temp");

				// Extracts first folder from the zip and moves it into appropriately named folder
				string modFolder = Directory.GetDirectories(installPath + "-temp")[0];
				Tools.MoveFilesAndDirs(modFolder, installPath);

				// Cleanup
				Directory.Delete(installPath + "-temp", true);
				File.Delete(downloadPath);
			});
		}
		catch (Exception installError)
		{
			_tools.ErrorPopup($@"failed to install mod:{installError}", _errorLabel, _errorPopup);
			throw;
		}

		return true;
	}


	private async Task<bool> UpdateMod(string gameId, string modName, string modUrl, Array<string> compatibleVersions, string currentVersion)
	{
		try
		{
			var removedMod = RemoveMod(gameId, modName, currentVersion, compatibleVersions).Result;
			if (removedMod != true)
			{
				return false;
			}
			await InstallMod(gameId, modName, modUrl, compatibleVersions);
			return true;
		}
		catch (Exception updateError)
		{
			_tools.ErrorPopup($@"failed to update mod:{updateError}", _errorLabel, _errorPopup);
			throw;
		}
	}


	private async Task<bool> RemoveMod(string gameId, string modName, string currentVersion, Array<string> compatibleVersions)
	{
		string modNameEnding = currentVersion == "N/A" ? "" : $"|{currentVersion}";
		string removePath = $@"{_modsPath}/{gameId}/{modName}{modNameEnding}";
		try
		{
			var confirm = await _tools.ConfirmationPopup(_confirmationPopup, $@"Delete {modName}?");
			if (confirm == false)
			{
				return false;
			}

			// Checks if the game is available for download, if not sets the bool to remove the game from the list
			YuzuMod modToRemove = null;
			foreach (var availableMod in _yuzuModsList[gameId])
			{
				if (availableMod.ModName == modName && availableMod.ModUrl != null)
				{
					modToRemove = null;
					break;
				}
				modToRemove = availableMod;
			}
			
			// If the mod was locally installed, removes it from the list of available mods.
			if (modToRemove != null)
			{
				_yuzuModsList[gameId].Remove(modToRemove);
			}

			// Deletes directory contents, then the directory itself.
			Tools.DeleteDirectoryContents(removePath);
			Directory.Delete(removePath, true);
			
			return true;
		}
		catch (Exception removeError)
		{
			_tools.ErrorPopup("failed to remove mod:" + removeError, _errorLabel, _errorPopup);
			return false;
		}
	}
	
	
	private void SelectGame(int gameIndex)
	{
		// Gets the keys we can equate as an array
		_currentGameId = GetGameIdFromValue(_gamePickerButton.GetItemText(gameIndex).Trim(), _installedGames);
		// Clears old mods from our list
		_modList.Clear();
		AddMods(_currentGameId);
	}
	
	
	static string GetGameIdFromValue(string value, Godot.Collections.Dictionary<string, Game> installedGames)
	{
		foreach (string gameId in installedGames.Keys)
		{
			if (installedGames[gameId].GameName == value)
			{
				return gameId;
			}
		}
		return null;
	}
	
	
	// Signal functions
	private async void ModClicked(int modIndex)
	{
		// Finds mod with the same name as the one clicked and installs it
		if (_yuzuModsList.TryGetValue(_currentGameId, out var value))
		{
			foreach (var mod in value)
			{
				if (_modList.GetItemText(modIndex).Split("-")[0].Trim() == (mod.ModName))
				{
					if (mod.IsInstalled)
					{
						mod.IsInstalled = !await RemoveMod(_currentGameId, mod.ModName, mod.CurrentVersion, mod.CompatibleVersions);
					}
					else
					{
						mod.IsInstalled = await InstallMod(_currentGameId, mod.ModName, mod.ModUrl, mod.CompatibleVersions);
					}

					// Used to update UI with installed icon
					SelectGame(_gamePickerButton.Selected);
				}
			}
		}
	}
	
	
	private async void UpdateSelectedPressed()
	{
		foreach (var mod in _yuzuModsList[_currentGameId])
		{
			var selectedMods = _modList.GetSelectedItems();
			if (selectedMods.Length <= 0)
			{
				return;
			}
			
			await Task.Run(async () =>
			{
				if (_modList.GetItemText(selectedMods[0]).Split("-")[0].Trim() == (mod.ModName))
				{
					if (mod.IsInstalled && mod.ModUrl != null)
					{
						await UpdateMod(_currentGameId, mod.ModName, mod.ModUrl, mod.CompatibleVersions,
							mod.CurrentVersion);
					}

					// Used to update UI with installed icon
					SelectGame(_gamePickerButton.Selected);
				}
			});
		}
	}

}





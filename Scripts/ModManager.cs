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
	[ExportGroup("ModManager")] 
	[Export()] private ItemList _modList;
	[Export()] private OptionButton _gamePickerButton;
	[Export()] private HttpRequest _titleRequester;
	[Export()] private Texture2D _installedIcon;
	
	private readonly System.Collections.Generic.Dictionary<string, List<(string, string)>> _availableGameMods = new System.Collections.Generic.Dictionary<string, List<(string, string)>>();
	
	private const string Quote = "\"";
	private string _currentGameId;
	private SettingsResource _settings;
	private ResourceSaveManager _saveManager = new ResourceSaveManager();
	private string _modsPath;

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
		AddMods();
	}


	// Custom functions
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
			if (!_availableGameMods.ContainsKey(gameId))
			{
				_availableGameMods[gameId] = new List<(string, string)>();
			}

			var modsSourcePage = htmlWeb.Load("https://github.com/yuzu-emu/yuzu/wiki/Switch-Mods");
			// List of elements, which MOSTLY contain mods (not all)
			var mods = modsSourcePage.DocumentNode.SelectNodes(
				$@"//h3[contains(., {Quote}{_settings.InstalledGames[gameId].GameName}{Quote})]/following::table[1]//td//a");
			if (mods == null)
			{
				return;
			}
			
			for (int searchIndex = 0; searchIndex < mods.Count; searchIndex++)
			{
				var mod = mods[searchIndex];
				string downloadUrl = mod.GetAttributeValue("href", null).Trim();
				string modName = mod.InnerText;
				if (downloadUrl.EndsWith(".rar") || downloadUrl.EndsWith(".zip") || downloadUrl.EndsWith(".7z"))
				{
					_availableGameMods[gameId].Add((modName, downloadUrl));
				}
			}
		});
		
	}


	// Rename to be more accurate abt how it also gets installed / available mods
	private async void GetGamesAndMods()
	{
		_titleRequester.Request(
			"https://switchbrew.org/w/index.php?title=Title_list/Games&mobileaction=toggle_view_desktop");
		await ToSignal(_titleRequester, "request_completed"); // Waits for titles to be retrieved before checking installed titles against them.
		
		foreach (var gameModFolder in Directory.GetDirectories(_modsPath))
		{
			string gameId = gameModFolder.GetFile(); // Gets game id by grabbing the folders name
			if (_settings.Titles.TryGetValue(gameId, out var gameName))
			{
				// Checks if title id is in installed games, if not adds it.
				if (!_settings.InstalledGames.ContainsKey(gameId))
				{
					_settings.InstalledGames[gameId] = new Game(gameName, new Godot.Collections.Dictionary<string, YuzuMod>());
				}
				_settings.InstalledGames[gameId].GameName = gameName;
				GetInstalledMods(gameId);
				await GetAvailableMods(gameId, false);
				_gamePickerButton.AddItem(gameName);
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

	
	private void GetInstalledMods(string gameId)
	{
		foreach (var mod in Directory.GetDirectories($@"{_modsPath}/{gameId}"))
		{
			string modName = mod.GetFile();
			_settings.InstalledGames[gameId].YuzuModsList.Add(modName, new YuzuMod("", 1f));
		}
	}


	// Adds available and local mods to mod list
	private void AddMods()
	{
		// Add online mods
		foreach (var gameId in _settings.InstalledGames.Keys)
		{
			foreach (var installedMod in _settings.InstalledGames[gameId].YuzuModsList)
			{
				_modList.AddItem(installedMod.Key, icon: _installedIcon);
			}

			// Adds non-installed mods to the list
			if (_availableGameMods.TryGetValue(gameId, out var gameMods))
			{
				foreach (var mod in gameMods)
				{
					// Checks if the mod is already installed, if so returns.
					if (_settings.InstalledGames.TryGetValue(gameId, out var game))
					{
						if (game.YuzuModsList.ContainsKey(mod.Item2))
						{
							continue;
						}
					}
					_modList.AddItem(mod.Item2);
				}
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
			_settings.Titles[gameSplit[1]] = gameSplit[2];
		}
	}


	private async void InstallMod(string gameId, string modName, string modUrl)
	{
		HttpClient httpClient = new HttpClient();
		byte[] modDownload = await httpClient.GetAsync(modUrl).Result.Content.ReadAsByteArrayAsync();
		// Should really make it so / is different for windows and linux TODO
		await System.IO.File.WriteAllBytesAsync($@"{_modsPath}/{gameId}/{modName}", modDownload);
	}


	private void SelectGame(int gameIndex)
	{
		// Gets the keys we can equate as an array
		var installedKeys = _settings.InstalledGames.Keys.ToArray();
		_currentGameId = GetGameIdFromValue(_gamePickerButton.GetItemText(gameIndex), _settings.InstalledGames);
		// Clears old mods from our list
		_modList.Clear();

		// Adds installed mods to the list
		if (_settings.InstalledGames.TryGetValue(_currentGameId, out var installedGame))
		{
			foreach (var mod in installedGame.YuzuModsList)
			{
				_modList.AddItem(mod.Key, icon: _installedIcon);
			}
		}
		
		// Adds non-installed mods to the list
		if (_availableGameMods.TryGetValue(_currentGameId, out var gameMod))
		{
			foreach (var mod in gameMod)
			{
				// Checks if the mod is already installed, if so returns.
				if (_settings.InstalledGames.TryGetValue(_currentGameId, out var installedGameToCompare))
				{
					if (installedGameToCompare.YuzuModsList.ContainsKey(mod.Item1))
					{
						continue;
					}
				}
				_modList.AddItem(mod.Item1);
			}
		}
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
	private void ModClicked(int modIndex)
	{
		// Finds mod with the same name as the one clicked and installs it
		foreach (var mod in _availableGameMods[_currentGameId])
		{
			if (_modList.GetItemText(modIndex) == mod.Item1)
			{
				InstallMod(_currentGameId, mod.Item1, mod.Item2);
			}
		}
	}

}


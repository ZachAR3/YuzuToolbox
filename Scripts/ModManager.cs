using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HttpClient = System.Net.Http.HttpClient;

public partial class ModManager : Control
{
	[ExportGroup("ModManager")] 
	[Export()] private ItemList _modList;
	[Export()] private OptionButton _gamePickerButton;
	[Export()] private HttpRequest _titleRequester;
	[Export()] private Texture2D _installedIcon;
	
	private readonly System.Collections.Generic.Dictionary<string, List<(int, string, string)>> _availableGameMods = new System.Collections.Generic.Dictionary<string, List<(int, string, string)>>();
	private Dictionary<string,  List<string>> _installedMods = new Dictionary<string, List<string>>();
	private const string Quote = "\"";
	private string _currentGameId;
	private SettingsResource _settings;
	private ResourceSaveManager _saveManager = new ResourceSaveManager();
	private string _modsPath;
	
	// Functions
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
		GetInstalledGames();
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
			if (!_availableGameMods.ContainsKey(gameId))
			{
				_availableGameMods[gameId] = new List<(int, string, string)>();
			}

			var modsSourcePage = htmlWeb.Load("https://github.com/yuzu-emu/yuzu/wiki/Switch-Mods");
			// List of elements, which MOSTLY contain mods (not all)
			var mods = modsSourcePage.DocumentNode.SelectNodes(
				$@"//h3[contains(., {Quote}{_settings.InstalledTitles[gameId]}{Quote})]/following::table[1]//td//a");
			if (mods == null)
			{
				return;
			}

			int modIndex = 0;
			for (int searchIndex = 0; searchIndex < mods.Count; searchIndex++)
			{
				var mod = mods[searchIndex];
				string downloadUrl = mod.GetAttributeValue("href", null).Trim();
				string modName = mod.InnerText;
				if (downloadUrl.EndsWith(".rar") || downloadUrl.EndsWith(".zip") || downloadUrl.EndsWith(".7z"))
				{
					_availableGameMods[gameId].Add((modIndex, modName, downloadUrl));
					_modList.AddItem(modName, icon: (!_installedMods.ContainsKey(gameId) || !_installedMods[gameId].Contains(modName)) ? _installedIcon : null);
					modIndex++;
				}
			}


			// Checks if querying banana mods or normal
			// if (useBananaMods)
			// {
			// 	int currentPage = 1;
			// 	string gameModsSource = httpClient
			// 		.GetAsync($@"https://gamebanana.com/apiv11/Game/{gameId}/Subfeed?_nPage={currentPage}").Result
			// 		.Content
			// 		.ReadAsStringAsync().Result;
			// 	var jsonMods = JObject.Parse(gameModsSource);
			//
			//
			// 	foreach (var mod in jsonMods["_aRecords"])
			// 	{
			// 		modIndex++;
			//
			// 		_availableGameMods[(int)gameId].Add((modIndex, mod["_sName"].ToString(), mod["_sProfileUrl"].ToString()));
			// 		_modList.AddItem(mod["_sName"].ToString());
			// 	}
			// }
		});
		
		SelectGame(0);
	}


	// Rename to be more accurate abt how it also gets installed / available mods
	private async void GetInstalledGames()
	{
		_titleRequester.Request(
			"https://switchbrew.org/w/index.php?title=Title_list/Games&mobileaction=toggle_view_desktop");
		await ToSignal(_titleRequester, "request_completed"); // Waits for titles to be retrieved before checking installed titles against them.
		
		foreach (var gameModFolder in Directory.GetDirectories(_modsPath))
		{
			string gameId = gameModFolder.GetFile(); // Gets game id by grabbing the folders name
			if (_settings.Titles.TryGetValue(gameId, out var title)) // Checks if title list contains the id, if so adds it to installed titles.
			{
				//TODO redo layouts of installed to not always need to mod id for bananagames
				//_settings.InstalledTitles[gameId] = (title, GetGameModId(title));
				_settings.InstalledTitles[gameId] = title;
				GetInstalledMods(gameId);
				await GetAvailableMods(gameId, false);
				_gamePickerButton.AddItem(title);
			}
			else
			{
				// TODO better solution for informing user
				GD.Print("Cannot find title:" + gameId);
			}

		}
	}

	
	private void GetInstalledMods(string gameId)
	{
		foreach (var mod in Directory.GetDirectories($@"{_modsPath}/{gameId}"))
		{
			string modName = mod.GetFile();
			if (!_installedMods.ContainsKey(gameId))
			{
				_installedMods[gameId] = new List<string>();
			}
			_installedMods[gameId].Add(modName);
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


	// private int? GetGameModId(string gameName)
	// {
	// 	HttpClient httpClient = new HttpClient();
	// 	// Searches for the game ID using the name from banana mods
	// 	string searchContent = httpClient.GetAsync("https://gamebanana.com/apiv11/Util/Game/NameMatch?_sName=" + gameName).Result.Content.ReadAsStringAsync().Result;
	// 	var jsonContent = JObject.Parse(searchContent);
	// 	return jsonContent["_aRecords"]?[0]?["_idRow"]!.ToString().ToInt();
	// }
	
	
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
		var installedKeys = _settings.InstalledTitles.Keys.ToArray();
		string gameId = installedKeys[gameIndex];
		_currentGameId = gameId;
		// Clears old mods from our list
		_modList.Clear();
		// Adds all mods from the designated game.
		foreach (var mod in _availableGameMods[gameId])
		{
			Texture2D installedIcon = null;
			if (_installedMods.ContainsKey(gameId))
			{
				installedIcon = _installedMods[gameId].Contains(mod.Item2) ? _installedIcon : null;
			}
			_modList.AddItem(mod.Item2, icon: installedIcon);
		}
	}
	
	
	// Signal functions
	private void ModClicked(int index)
	{
		foreach (var mod in _availableGameMods[_currentGameId])
		{
			if (index == mod.Item1)
			{
				InstallMod(_currentGameId, mod.Item2, mod.Item3);
			}
		}
	}
}


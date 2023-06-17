using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot.Collections;
using HtmlAgilityPack;
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
	[Export()] private OptionButton _sourcePickerButton;
	[Export()] private HttpRequest _titleRequester;
	[Export()] private Texture2D _installedIcon;
	[Export()] private Button _modLocationButton;
	[Export()] private Panel _loadingPanel;

	private const string Quote = "\"";
	private string _currentGameId;
	private Tools _tools = new Tools();
	private string _osUsed = OS.GetName();

	enum Sources
	{
		Official,
		Banana
	}
	private Dictionary<string, Array<Mod>> _selectedSourceMods = new Dictionary<string, Array<Mod>>();
	
	// Game id, game name
	private Godot.Collections.Dictionary<string, string> _titles = new Godot.Collections.Dictionary<string, string>();
	// Game id, mod names array
	private Dictionary<string, Game> _installedGames = new Godot.Collections.Dictionary<string, Game>();
	private Dictionary<string, Array<Mod>> _officialModList = new Godot.Collections.Dictionary<string, Array<Mod>>();

	// Godot Functions
	private void Initiate()
	{	
		_titleRequester.Connect("request_completed", new Callable(this, nameof(GetTitles)));

		if (Globals.Instance.Settings.ModsLocation == null)
		{
			Globals.Instance.Settings.ModsLocation = $@"{Globals.Instance.Settings.AppDataPath}load";
			Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
		}
		_modLocationButton.Text = Globals.Instance.Settings.ModsLocation.PadLeft(Globals.Instance.Settings.ModsLocation.Length + 4, ' ');
		
		// TODO add any other custom sources to the list
		
		GetGamesAndMods();
	}


	// Custom functions
	private async void GetGamesAndMods()
	{
		await Task.Run(async () =>
		{
			_loadingPanel.Visible = true;
			if (!Directory.Exists(Globals.Instance.Settings.ModsLocation))
			{
				_tools.ErrorPopup($@"mods directory not found", _errorLabel, _errorPopup);
				_loadingPanel.Visible = false;
				return;
			}
			
			_titleRequester.Request(
				"https://switchbrew.org/w/index.php?title=Title_list/Games&mobileaction=toggle_view_desktop");
			await ToSignal(_titleRequester,
				"request_completed"); // Waits for titles to be retrieved before checking installed titles against them.

			// Checks if no titles were found, if they weren't gives error and cancels.
			if (_titles.Count <= 0)
			{
				_tools.ErrorPopup("failed to retrieve titles list, check connection and try again later.", _errorLabel, _errorPopup);
				_loadingPanel.Visible = false;
				return;
			}
			
			foreach (var gameModFolder in Directory.GetDirectories(Globals.Instance.Settings.ModsLocation))
			{
				string gameId = gameModFolder.GetFile(); // Gets game id by grabbing the folders name
				if (_titles.TryGetValue(gameId, out var gameName))
				{
					_installedGames[gameId] = new Game(gameName);
					if (!_officialModList.ContainsKey(gameId))
					{
						_officialModList[gameId] = new Array<Mod>();
					}

					await Task.Run(() => GetAvailableMods(gameId));
					GetInstalledMods(gameId);
					_gamePickerButton.AddItem($@"    {gameName}");
				}
				else
				{
					// TODO: Find a better solution for informing user
					GD.Print("Cannot find title:" + gameId);
				}

			}

			// Sets the first game as selected by default
			if (_installedGames.Count > 0)
			{
				SelectGame(0);
			}
			else
			{
				_tools.ErrorPopup("no installed games found", _errorLabel, _errorPopup);
				_loadingPanel.Visible = false;
			}
		});
	}

	
	private async Task GetAvailableMods(string gameId, int source = (int)Sources.Official)
	{
		if (gameId == null || !_installedGames.ContainsKey(gameId))
		{
			_tools.ErrorPopup("no game ID given or invalid. Cancelling...", _errorLabel, _errorPopup);
			_loadingPanel.Visible = false;
			return;
		}

		// Grabs the mods from the specified source.
		switch (source)
		{
			case (int)Sources.Official:
				OfficialManager _officialManager = new OfficialManager();
				try
				{
					_officialModList = await _officialManager.GetAvailableMods(_officialModList, _installedGames, gameId, (int)Sources.Official);
				}
				catch (ArgumentException argumentException)
				{
					_tools.ErrorPopup($@"Failed to retrieve mod list for ID:{gameId} | Title:{_titles[gameId]}. The game may not have available mods.", _errorLabel, _errorPopup);
					_loadingPanel.Visible = false;
					return; // TODO
				}

				break;
			
			case (int)Sources.Banana:
				break;
		}

	}


	private void GetInstalledMods(string gameId)
	{
		try
		{
			foreach (var modDirectory in Directory.GetDirectories($@"{Globals.Instance.Settings.ModsLocation}/{gameId}"))
			{
				string[] modInfo = modDirectory.GetFile().Split("!");
				string modName = modInfo[0];
				string modVersion = modInfo.Length > 1 ? modInfo[1] : "N/A";
				int modSource = modInfo.Length > 2 ? modInfo[2].ToInt() : -1;
				bool available = false;

				Dictionary<string, Array<Mod>> modSourceList = new Dictionary<string, Array<Mod>>();
				switch (modSource)
				{
					case (int)Sources.Official:
						modSourceList = _officialModList;
						break;
					case (int)Sources.Banana:
						// Add code for banana list
						break;
					// Returns if nothing is found.
					default:
						return;
				}
				foreach (var availableMod in modSourceList[gameId])
				{
					// Extra replacements are used since Windows can't handle ":" we need to convert it back and forth in names to periods.
					if (availableMod.ModName.Contains(modName) || availableMod.ModName.Contains(modName.Replace(".", ":")))
					{
						availableMod.IsInstalled = true;
						availableMod.CurrentVersion = modVersion;
						availableMod.Source = modSource;
						available = true;
						break;
					}
				}

				if (!available)
				{
					// Sets the version as NA since it can't be found online.
					var versions = new Array<string>();
					versions.Add("N/A");
					
					modSourceList[gameId].Add(new Mod(modName, null, versions, modVersion, true, modSource));
				}
			}
		}
		catch (Exception installedError)
		{
			_tools.ErrorPopup($@"cannot find installed mods error: {installedError}", _errorLabel, _errorPopup);
			_loadingPanel.Visible = false;
			throw;
		}
	}


	// Adds available and local mods to mod list
	private void AddMods(string gameId)
	{
		if (!_officialModList.ContainsKey(gameId))
		{
			GD.Print("Cannot find games for:" + gameId);
			_loadingPanel.Visible = false;
			return;
		}
		
		foreach (var mod in _officialModList[gameId])
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
		
		_loadingPanel.Visible = false;
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
			var gameSplit = gameCleaned.Split("\n");

			if (gameSplit.Length < 2)
			{
				_tools.ErrorPopup("unable to parse titles list, check connection and try again later.", _errorLabel, _errorPopup);
				_loadingPanel.Visible = false;
				return;
			}
			// Adds the game to our title list with type Dictionary(string ID, string Title, string modID)
			_titles[gameSplit[1]] = gameSplit[2];
		}
	}

	
	private async void UpdateAll()
	{
		var confirm = await _tools.ConfirmationPopup(_confirmationPopup, $@"Update all mods?");
		if (confirm == false)
		{
			return;
		}
		
		foreach (var installedGame in _installedGames)
		{
			foreach (var mod in _officialModList[installedGame.Key])
			{
				if (mod.IsInstalled && mod.ModUrl != null)
				{
					await Task.Run(async () =>
					{
						_loadingPanel.Visible = true;
						var modUpdated = await UpdateMod(installedGame.Key, mod.ModName, mod.ModUrl,
							mod.CompatibleVersions, mod.CurrentVersion, mod.Source, true);
						if (modUpdated != true)
						{
							_loadingPanel.Visible = false;
							return;
						}

						_modList.AddItem($@"  {mod.ModName} - Supports:{mod.CompatibleVersions}  ",
							icon: _installedIcon);
						// Used to update UI with installed icon
						SelectGame(_gamePickerButton.Selected);
						
						_loadingPanel.Visible = false;
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
				// Gets download path, and if using windows replaces /'s with \'s
				string downloadPath = _osUsed == "Linux"
					? $@"{Globals.Instance.Settings.ModsLocation}/{gameId}/{modName}-Download"
					: $@"{Globals.Instance.Settings.ModsLocation}\{gameId}\{modName.Replace(":", ".")}-Download";

					// Gets install path, and if using windows replaces /'s with \'s
				string installPath = _osUsed == "Linux" 
					? $@"{Globals.Instance.Settings.ModsLocation}/{gameId}/{modName}!{compatibleVersions.Last()}" 
					: $@"{Globals.Instance.Settings.ModsLocation}\{gameId}\{modName.Replace(":", ".")}!{compatibleVersions.Last()}";

				HttpClient httpClient = new HttpClient();
				byte[] downloadData = await httpClient.GetAsync(modUrl).Result.Content.ReadAsByteArrayAsync();
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
			_loadingPanel.Visible = false;
			throw;
		}

		return true;
	}


	private async Task<bool> UpdateMod(string gameId, string modName, string modUrl, Array<string> compatibleVersions, string currentVersion, int source, bool noConfirmation = false)
	{
		if (!noConfirmation)
		{
			var confirm = await _tools.ConfirmationPopup(_confirmationPopup, $@"Update {modName}?");
			if (confirm == false)
			{
				return false;
			}
		}

		try
		{
			var removedMod = RemoveMod(gameId, modName, currentVersion, compatibleVersions, source, true).Result;
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
			_loadingPanel.Visible = false;
			throw;
		}
	}


	private async Task<bool> RemoveMod(string gameId, string modName, string currentVersion, Array<string> compatibleVersions, int modSource, bool noConfirmation = false)
	{
		string modNameEnding = currentVersion == "N/A" ? "" : $"!{currentVersion}";
		string removePath = _osUsed == "Linux" 
			? $@"{Globals.Instance.Settings.ModsLocation}/{gameId}/{modName}!{compatibleVersions.Last()}" 
			: $@"{Globals.Instance.Settings.ModsLocation}\{gameId}\{modName.Replace(":", ".")}!{compatibleVersions.Last()}";;
		try
		{
			if (!noConfirmation)
			{
				var confirm = await _tools.ConfirmationPopup(_confirmationPopup, $@"Delete {modName}?");
				if (confirm == false)
				{
					return false;
				}
			}

			Dictionary<string, Array<Mod>> modSourceList = new Dictionary<string, Array<Mod>>();
			switch (modSource)
			{
				case (int)Sources.Official:
					modSourceList = _officialModList;
					break;
				case (int)Sources.Banana:
					// Add code for banana list
					break;
			}
			// Checks if the game is available for download, if not sets the bool to remove the game from the list
			Mod modToRemove = null;
			foreach (var availableMod in modSourceList[gameId])
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
				modSourceList[gameId].Remove(modToRemove);
			}

			// Deletes directory contents, then the directory itself.
			Tools.DeleteDirectoryContents(removePath);
			Directory.Delete(removePath, true);
			
			return true;
		}
		catch (Exception removeError)
		{
			_tools.ErrorPopup("failed to remove mod:" + removeError, _errorLabel, _errorPopup);
			_loadingPanel.Visible = false;
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
		if (!_selectedSourceMods.ContainsKey(_currentGameId))
		{
			return;
		}

		foreach (var mod in _selectedSourceMods[_currentGameId])
		{
			if (_modList.GetItemText(modIndex).Split("-")[0].Trim() == (mod.ModName))
			{
				if (mod.IsInstalled)
				{
					mod.IsInstalled = !await RemoveMod(_currentGameId, mod.ModName, mod.CurrentVersion, mod.CompatibleVersions, mod.Source);
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
	
	
	private async void UpdateSelectedPressed()
	{
		foreach (var mod in _officialModList[_currentGameId])
		{
			var selectedMods = _modList.GetSelectedItems();
			if (selectedMods.Length <= 0)
			{
				return;
			}
			
			_loadingPanel.Visible = true;
			
			if (_modList.GetItemText(selectedMods[0]).Split("-")[0].Trim() == (mod.ModName))
			{
				if (mod.IsInstalled && mod.ModUrl != null)
				{
					await UpdateMod(_currentGameId, mod.ModName, mod.ModUrl, mod.CompatibleVersions,
						mod.CurrentVersion, mod.Source);
				}

				// Used to update UI with installed icon
				SelectGame(_gamePickerButton.Selected);
				_loadingPanel.Visible = false;
			}
		}
	}
	
	
	private void ModLocationPressed()
	{
		var modLocationInput = _tools.OpenFileChooser(Globals.Instance.Settings.ModsLocation, _errorLabel, _errorPopup)
			.Result;
		if (modLocationInput != null)
		{
			Globals.Instance.Settings.ModsLocation = modLocationInput;
		}
		
		_modLocationButton.Text = Globals.Instance.Settings.ModsLocation.PadLeft(Globals.Instance.Settings.ModsLocation.Length + 4, ' ');
		
		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
	}


	private void RefreshPressed()
	{
		// Clean up
		_modList.Clear();
		_gamePickerButton.Clear();
		_officialModList.Clear();
		_installedGames.Clear();
		_titles.Clear();

		// Re-grabs and adds the mods
		GetGamesAndMods();
	}
}

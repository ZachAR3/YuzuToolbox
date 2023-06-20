using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot.Collections;
using HttpClient = System.Net.Http.HttpClient;

public partial class ModManager : Control
{
	[ExportGroup("General")]
	[Export()] private Popup _errorPopup;
	[Export()] private Label _errorLabel;
	[Export()] private PopupMenu _confirmationPopup;

	[ExportGroup("ModManager")] 
	[Export()] private int _selectionPaddingLeft = 4;
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


	// Code for handling sources and their associated names with each
	enum Sources
	{
		Official,
		Banana,
		All
	}
	
	private Array<string> _sourceNames = new Array<string>();
	private Godot.Collections.Dictionary<string, Array<Mod>> _selectedSourceMods = new Godot.Collections.Dictionary<string, Array<Mod>>();
	private int _selectedSource = (int)Sources.Official;
	
	// Game id, game name
	private Godot.Collections.Dictionary<string, string> _titles = new Godot.Collections.Dictionary<string, string>();
	// Game id, mod names array
	private Godot.Collections.Dictionary<string, Game> _installedGames = new Godot.Collections.Dictionary<string, Game>();
	private Godot.Collections.Dictionary<string, Array<Mod>> _availableMods = new Godot.Collections.Dictionary<string, Array<Mod>>();
	private Godot.Collections.Dictionary<string, Array<Mod>> _installedMods = new Godot.Collections.Dictionary<string, Array<Mod>>();

	// Godot Functions
	private void Initiate()
	{	
		_titleRequester.Connect("request_completed", new Callable(this, nameof(GetTitles)));

		_sourceNames.Insert((int)Sources.Official, "Official");
		_sourceNames.Insert((int)Sources.Banana, "Banana");

		if (Globals.Instance.Settings.ModsLocation == null)
		{
			Globals.Instance.Settings.ModsLocation = $@"{Globals.Instance.Settings.AppDataPath}load";
			Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
		}
		_modLocationButton.Text = Globals.Instance.Settings.ModsLocation.PadLeft(Globals.Instance.Settings.ModsLocation.Length + _selectionPaddingLeft, ' ');
		
		AddSources();
		GetGamesAndMods();
	}


	// Custom functions
	private async void GetGamesAndMods(int source = (int)Sources.Official)
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
					await Task.Run(() => GetAvailableMods(gameId, source));
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

	
	private async Task GetAvailableMods(string gameId, int source)
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
				OfficialGrabber officialManager = new OfficialGrabber();
				try
				{
					_selectedSourceMods = await officialManager.GetAvailableMods(_selectedSourceMods, _installedGames, gameId, (int)Sources.Official);
					_availableMods = _selectedSourceMods;
				}
				catch (ArgumentException argumentException)
				{
					_tools.ErrorPopup($@"Failed to retrieve mod list for ID:{gameId} | Title:{_titles[gameId]}. The game may not have available mods.", _errorLabel, _errorPopup);
					_loadingPanel.Visible = false;
				}

				break;
			
			case (int)Sources.Banana:
				BananaGrabber bananaGrabber = new BananaGrabber();
				try
				{
					_selectedSourceMods = await bananaGrabber.GetAvailableMods(_selectedSourceMods, _installedGames, gameId, (int)Sources.Banana);
					_availableMods = _selectedSourceMods;
				}
				catch (ArgumentException argumentException)
				{
					_tools.ErrorPopup($@"Failed to retrieve mod list for ID:{gameId} | Title:{_titles[gameId]}. The game may not have available mods.", _errorLabel, _errorPopup);
					_loadingPanel.Visible = false;
				}

				break;
				
			case (int)Sources.All:
				// Code for getting all available mods and adding it to selected source mods
				break;
		}

	}


	private void GetInstalledMods(string gameId)
	{
		try
		{
			// Initializes installed mods for the given title.
			_installedMods[gameId] = new Array<Mod>();
			
			foreach (var modDirectory in Directory.GetDirectories($@"{Globals.Instance.Settings.ModsLocation}/{gameId}"))
			{
				string[] modInfo = modDirectory.GetFile().Split("!");
				string modName = modInfo[0];
				string modVersion = modInfo.Length > 1 ? modInfo[1] : "N/A";
				var compatibleVersions = new Array<string> { modVersion };
				int modSource = modInfo.Length > 2 ? _sourceNames.IndexOf(modInfo[2]) : -1;

				Mod availableMod = IsModAvailable(gameId, modName, modSource);

				// If the mod isn't found in any online sources sets it to be just be a local mod with no source or url.
				if (availableMod == null)
				{
					_installedMods[gameId].Add(new Mod(modName, null, compatibleVersions, -1, modDirectory));
					return;
				}

				// Sets the installed location
				availableMod.InstalledPath = modDirectory;
				
				_installedMods[gameId].Add(availableMod);
				_availableMods[gameId].Remove(availableMod);
			}
		}
		catch (Exception installedError)
		{
			_tools.ErrorPopup($@"cannot find installed mods error: {installedError}", _errorLabel, _errorPopup);
			_loadingPanel.Visible = false;
			throw;
		}
	}


	// Checks if a mod is available for download, if so returns the mod
	private Mod IsModAvailable(string gameId, string modName, int source = -1)
	{
		if (_availableMods.TryGetValue(gameId, out var availableMods))
		{
			foreach (Mod availableMod in availableMods)
			{
				if (availableMod.ModName == modName || availableMod.ModName == modName.Replace(".", ":"))
				{
					if (source != -1 && source != availableMod.Source)
					{
						continue;
					}

					_availableMods[gameId].Remove(availableMod);
					return availableMod;
				}
			}
		}

		return null;
	}
	

	// Adds available and local mods to mod list
	private void AddMods(string gameId)
	{
		if (!_selectedSourceMods.ContainsKey(gameId))
		{
			GD.Print("Cannot find games for:" + gameId);
			_loadingPanel.Visible = false;
			return;
		}
		
		// Adds the available and installed mods
		if (_installedMods.TryGetValue(gameId, out var installedMods))
		{
			foreach (var mod in installedMods)
			{
				_modList.AddItem($@"  {mod.ModName} - Supports:{string.Join(", ", mod.CompatibleVersions)}  ",
					icon: _installedIcon);
			}
		}

		if (_selectedSourceMods.TryGetValue(gameId, out var selectedSourceMods))
		{
			foreach (var mod in selectedSourceMods)
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
			// Mods list is temporarily duplicated to avoid issues with indexing when removing and re-adding the mods during update.
			foreach (var mod in _installedMods[installedGame.Key].Duplicate())
			{
				if (mod.ModUrl != null)
				{
					_loadingPanel.Visible = true;
					var modUpdated = await UpdateMod(installedGame.Key, mod, true);
					if (modUpdated != true)
					{
						_tools.ErrorPopup($@"failed to update:{mod.ModName}", _errorLabel, _errorPopup);
						_loadingPanel.Visible = false;
						return;
					}
			
					SelectGame(_gamePickerButton.Selected);
					_loadingPanel.Visible = false;
				}
			}
		}
	}
	
	
	private async Task<bool> UpdateMod(string gameId, Mod mod, bool noConfirmation = false)
	{
		if (!noConfirmation)
		{
			var confirm = await _tools.ConfirmationPopup(_confirmationPopup, $@"Update {mod.ModName}?");
			if (confirm == false)
			{
				return false;
			}
		}

		try
		{
			var removedMod = RemoveMod(gameId, mod, true).Result;
			if (removedMod != true)
			{
				return false;
			}
			await InstallMod(gameId, mod);
			return true;
		}
		catch (Exception updateError)
		{
			_tools.ErrorPopup($@"failed to update mod:{updateError}", _errorLabel, _errorPopup);
			_loadingPanel.Visible = false;
			throw;
		}
	}

	

	private async Task<bool> InstallMod(string gameId, Mod mod)
	{
		try
		{
			await Task.Run(async () =>
			{
				GD.Print(mod.ModUrl);
				// Gets download path, and if using windows replaces /'s with \'s
				string downloadPath = _osUsed == "Linux"
					? $@"{Globals.Instance.Settings.ModsLocation}/{gameId}/{mod.ModName}-Download"
					: $@"{Globals.Instance.Settings.ModsLocation}\{gameId}\{mod.ModName.Replace(":", ".")}-Download";

					// Gets install path, and if using windows replaces /'s with \'s
				string installPath = _osUsed == "Linux" 
					? $@"{Globals.Instance.Settings.ModsLocation}/{gameId}/{mod.ModName}!{mod.CompatibleVersions.Last()}!{mod.Source}" 
					: $@"{Globals.Instance.Settings.ModsLocation}\{gameId}\{mod.ModName.Replace(":", ".")}!{mod.CompatibleVersions.Last()}!{_sourceNames[mod.Source]}";

				HttpClient httpClient = new HttpClient();
				byte[] downloadData = await httpClient.GetAsync(mod.ModUrl).Result.Content.ReadAsByteArrayAsync();
				await File.WriteAllBytesAsync(downloadPath, downloadData);
				System.IO.Compression.ZipFile.ExtractToDirectory(downloadPath, installPath + "-temp");

				// Extracts first folder from the zip and moves it into appropriately named folder
				string modFolder = Directory.GetDirectories(installPath + "-temp")[0];
				Tools.MoveFilesAndDirs(modFolder, installPath);

				// Cleanup
				Directory.Delete(installPath + "-temp", true);
				File.Delete(downloadPath);

				// Sets the installed path and initializes the installed mods list for the given game if needed
				mod.InstalledPath = installPath;
				_installedMods[_currentGameId] = !_installedMods.ContainsKey(_currentGameId)
					? new Array<Mod>()
					: _installedMods[_currentGameId];
				_installedMods[_currentGameId].Add(mod);
				
				_availableMods[_currentGameId].Remove(mod);
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
	

	private async Task<bool> RemoveMod(string gameId, Mod mod, bool noConfirmation = false)
	{
		try
		{
			if (!noConfirmation)
			{
				var confirm = await _tools.ConfirmationPopup(_confirmationPopup, $@"Delete {mod.ModName}?");
				if (confirm == false)
				{
					return false;
				}
			}

			if (mod.ModUrl != null)
			{
				_installedMods[gameId].Remove(mod);

				// If there is no mod list for the game id creates one
				_availableMods[gameId] =
					!_availableMods.ContainsKey(gameId) ? new Array<Mod>() : _availableMods[gameId];
				_availableMods[gameId].Add(mod);
				
				// Deletes directory contents, then the directory itself.
				Tools.DeleteDirectoryContents(mod.InstalledPath);
				Directory.Delete(mod.InstalledPath, true);
				
				// Refreshes the mod list
				SelectGame(_gamePickerButton.Selected);
			}

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


	private void Refresh(int source = (int)Sources.Official)
	{
		// Clean up
		_modList.Clear();
		_gamePickerButton.Clear();
		_selectedSourceMods.Clear();
		_installedGames.Clear();
		_titles.Clear();

		// Re-grabs and adds the mods
		GetGamesAndMods(source);
	}


	private void AddSources()
	{
		foreach (string source in _sourceNames)
		{
			_sourcePickerButton.AddItem(source.PadLeft(source.Length + _selectionPaddingLeft));
		}
	}
	
	
	// Signal functions
	private async void ModClicked(int modIndex)
	{
		// If the mod is found in the installed mods list removes it
		if (_installedMods.TryGetValue(_currentGameId, out var installedMods))
		{
			foreach (var mod in installedMods)
			{
				if (_modList.GetItemText(modIndex).Split("-")[0].Trim() == (mod.ModName))
				{
					await RemoveMod(_currentGameId, mod);
					return;
				}
			}
		}

		// Installs the mod from the online source
		if (_selectedSourceMods.TryGetValue(_currentGameId, out var selectedSourceMods))
		{
			foreach (var mod in selectedSourceMods)
			{
				if (_modList.GetItemText(modIndex).Split("-")[0].Trim() == (mod.ModName))
				{
					await InstallMod(_currentGameId, mod);
					break;
				}
			}
			// Used to update UI with installed icon	
			SelectGame(_gamePickerButton.Selected);
		}
	}


	// Refreshes the mods with the newly selected source
	private void SelectSource(int sourceIndex)
	{
		_selectedSource = _sourceNames.IndexOf(_sourcePickerButton.GetItemText(sourceIndex).Trim());
		if (_selectedSource == -1)
		{
			_tools.ErrorPopup("source not found, please file a bug report. Defaulting back to official", _errorLabel, _errorPopup);
			_sourcePickerButton.Select(0);
			return;
		}
		
		Refresh(_selectedSource);
	}
	
	
	private async void UpdateSelectedPressed()
	{
		foreach (Mod mod in _installedMods[_currentGameId])
		{
			var selectedMods = _modList.GetSelectedItems();
			if (selectedMods.Length <= 0)
			{
				return;
			}
			
			_loadingPanel.Visible = true;
			
			if (_modList.GetItemText(selectedMods[0]).Split("-")[0].Trim() == (mod.ModName))
			{
				if (mod.ModUrl != null)
				{
					await UpdateMod(_currentGameId, mod);
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
		
		_modLocationButton.Text = Globals.Instance.Settings.ModsLocation.PadLeft(Globals.Instance.Settings.ModsLocation.Length + _selectionPaddingLeft, ' ');
		
		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
	}


	private void RefreshPressed()
	{
		Refresh(_selectedSource);
	}
}

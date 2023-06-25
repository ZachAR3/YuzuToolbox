using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Common;
using YuzuEAUpdateManager.Scripts.Sources;

public partial class ModManager : Control
{
	[ExportGroup("General")] [Export()] private Tools _tools;
	[ExportGroup("ModManager")] [Export()] private string _installedModsPath;
	[Export()] private int _selectionPaddingLeft = 4;
	[Export()] private ItemList _modList;
	[Export()] private ProgressBar _downloadBar;
	[Export()] private Timer _downloadUpdateTimer;
	[Export()] private HttpRequest _downloadRequester;
	[Export()] private HttpRequest _titleRequester;
	[Export()] private Texture2D _installedIcon;
	[Export()] private Panel _loadingPanel;
	[Export()] private OptionButton _gamePickerButton;
	[Export()] private OptionButton _sourcePickerButton;
	[Export()] private Button _modLocationButton;
	[Export()] private Button _refreshButton;
	[Export()] private Button _updateAllButton;
	[Export()] private Button _updateSelectedButton;
	[Export()] private Button _loadMoreButton;


	private string _currentGameId;
	private string _osUsed = OS.GetName();
	private const string TitleSplitter = "||";


	// Code for handling sources and their associated names with each
	enum Sources
	{
		Official,
		Banana,
		TotkHolo,
		All
	}

	private List<string> _sourceNames = new();
	private Dictionary<string, List<Mod>> _selectedSourceMods = new();
	private int _selectedSource = (int)Sources.Official;

	// Game id, game name
	private Dictionary<string, string> _titles = new();

	// Game id, mod names List
	private Dictionary<string, Game> _installedGames = new();
	private Dictionary<string, List<Mod>> _installedMods = new();

	private StandardModManagement _standardModManager;
	private BananaManager _bananaManager = new();
	private OfficialManager _officialManager = new();
	private TotkHoloManager _totkHoloManager = new();

	private int _modsPage = 1;


	// Godot Functions
	private void Initiate()
	{
		// Converts the given local path to an absolute one upon run time
		_installedModsPath = ProjectSettings.GlobalizePath(_installedModsPath);

		_loadMoreButton.Disabled = true;

		_titleRequester.Connect("request_completed", new Callable(this, nameof(GetTitles)));

		_sourceNames.Insert((int)Sources.Official, "Official");
		_sourceNames.Insert((int)Sources.Banana, "Banana");
		_sourceNames.Insert((int)Sources.TotkHolo, "TOTKHolo");
		_sourceNames.Insert((int)Sources.All, "All");

		_modLocationButton.Text =
			Globals.Instance.Settings.ModsLocation.PadLeft(
				Globals.Instance.Settings.ModsLocation.Length + _selectionPaddingLeft, ' ');

		AddSources();
		GetGamesAndMods();
	}


	public override void _Notification(int notification)
	{
		if (notification == NotificationWMCloseRequest)
			SaveInstalledMods();
	}


	public void ResetInstalled()
	{
		_installedMods = new Dictionary<string, List<Mod>>();
		SaveInstalledMods();
	}


	// Custom functions
	private async void GetGamesAndMods(int source = (int)Sources.Official)
	{
		await Task.Run(async () =>
		{
			_loadingPanel.Visible = true;
			if (!Directory.Exists(Globals.Instance.Settings.ModsLocation))
			{
				_tools.ErrorPopup($@"mods directory not found");
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
				_tools.ErrorPopup("failed to retrieve titles list, check connection and try again later.");
				_loadingPanel.Visible = false;
				return;
			}

			foreach (var gameModFolder in Directory.GetDirectories(Globals.Instance.Settings.ModsLocation))
			{
				string gameId = gameModFolder.GetFile(); // Gets game id by grabbing the folders name
				if (_titles.TryGetValue(gameId, out var gameName))
				{
					_installedGames[gameId] = new() { GameName = gameName };
					GetInstalledMods(gameId);
					await Task.Run(async () => await GetAvailableMods(gameId, source));
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
				_tools.ErrorPopup("no installed games found");
				_loadingPanel.Visible = false;
			}
		});
	}


	private async Task GetAvailableMods(string gameId, int source)
	{
		if (gameId == null || !_installedGames.ContainsKey(gameId))
		{
			_tools.ErrorPopup("no game ID given or invalid. Cancelling...");
			_loadingPanel.Visible = false;
			return;
		}

		// Grabs the mods from the specified source.
		switch (source)
		{
			case (int)Sources.Official:
				try
				{
					_selectedSourceMods = await _officialManager.GetAvailableMods(_selectedSourceMods, _installedGames,
						gameId, (int)Sources.Official);
				}
				catch (ArgumentException argumentException)
				{
					_tools.ErrorPopup(
						$@"Failed to retrieve mod list for ID:{gameId} | Title:{_titles[gameId]}. The game may not have available mods.");
					_loadingPanel.Visible = false;
				}

				break;

			case (int)Sources.Banana:
				try
				{
					_selectedSourceMods = await _bananaManager.GetAvailableMods(_selectedSourceMods, _installedGames,
						gameId, (int)Sources.Banana, _modsPage);
				}
				catch (ArgumentException argumentException)
				{
					_tools.ErrorPopup(
						$@"Failed to retrieve mod list for ID:{gameId} | Title:{_titles[gameId]}. The game may not have available mods.");
					_loadingPanel.Visible = false;
				}

				break;

			case (int)Sources.TotkHolo:
				try
				{
					_selectedSourceMods =
						await _totkHoloManager.GetAvailableMods(_selectedSourceMods, gameId, (int)Sources.TotkHolo);
				}
				catch (ArgumentException argumentException)
				{
					_tools.ErrorPopup(
						$@"Failed to retrieve mod list for ID:{gameId} | Title:{_titles[gameId]}. The game may not have available mods.");
					_loadingPanel.Visible = false;
				}

				break;

			case (int)Sources.All:
				try
				{
					// Adds both official and banana mods the our source list
					_selectedSourceMods = await _officialManager.GetAvailableMods(_selectedSourceMods, _installedGames,
						gameId, (int)Sources.Official);
					_selectedSourceMods = await _bananaManager.GetAvailableMods(_selectedSourceMods, _installedGames,
						gameId, (int)Sources.Banana, _modsPage);
				}
				catch (ArgumentException argumentException)
				{
					_tools.ErrorPopup(
						$@"Failed to retrieve mod list for ID:{gameId} | Title:{_titles[gameId]}. The game may not have available mods.");
					_loadingPanel.Visible = false;
				}

				break;
		}

		// Really inefficient system to remove installed mods from available based on the name TODO
		if (_selectedSourceMods.ContainsKey(gameId))
		{
			foreach (var mod in _installedMods[gameId])
			{
				foreach (var selectedSourceMod in new List<Mod>(_selectedSourceMods[gameId]))
				{
					if (selectedSourceMod.ModName == mod.ModName)
					{
						_selectedSourceMods[gameId].Remove(selectedSourceMod);
					}
				}
			}
		}
	}


	private void GetInstalledMods(string gameId)
	{
		try
		{
			// Initializes the list of mods for the given game,
			_installedMods[gameId] = new List<Mod>();

			if (File.Exists(_installedModsPath))
			{
				var installedModsJson =
					JsonSerializer.Deserialize<Dictionary<string, List<Mod>>>(File.ReadAllText(_installedModsPath));
				if (installedModsJson.TryGetValue(gameId, out var gameMods))
				{
					_installedMods[gameId] = gameMods;
				}
			}

			// Adds local mods that aren't in the data base
			foreach (var modDirectory in
			         Directory.GetDirectories($@"{Globals.Instance.Settings.ModsLocation}/{gameId}"))
			{
				if (!modDirectory.GetFile().StartsWith("Managed"))
				{
					Mod modToAdd = new()
					{
						ModName = modDirectory.GetFile(),
						ModUrl = null,
						CompatibleVersions = new List<string> { "NA" },
						Source = -1,
						InstalledPath = modDirectory
					};

					if (_installedMods[gameId].Any(mod => mod.InstalledPath == modToAdd.InstalledPath))
					{
						return;
					}

					_installedMods[gameId].Add(modToAdd);
				}
			}
		}
		catch (Exception installedError)
		{
			_tools.ErrorPopup($@"cannot find installed mods error: {installedError}");
			_loadingPanel.Visible = false;
			throw;
		}

		SaveInstalledMods();
	}


	private async void LoadNextPage()
	{
		_modsPage++;
		_loadingPanel.Visible = true;
		DisableInteraction();

		var tempModsList = new Dictionary<string, List<Mod>>();
		await Task.Run(async () =>
		{
			switch (_selectedSource)
			{
				case (int)Sources.Official:
					break;
				case (int)Sources.Banana:
					tempModsList = await _bananaManager.GetAvailableMods(_selectedSourceMods, _installedGames,
						_currentGameId,
						_selectedSource, _modsPage);
					break;
				case (int)Sources.All:
					tempModsList = await _bananaManager.GetAvailableMods(_selectedSourceMods, _installedGames,
						_currentGameId,
						_selectedSource, _modsPage);
					break;
			}
		});

		// If our old list is the same as the new one, disabled the load more as no more mods are available.
		if (tempModsList == _selectedSourceMods)
		{
			_loadMoreButton.Disabled = true;
		}
		else
		{
			_selectedSourceMods = tempModsList;
		}

		DisableInteraction(false);
		_loadingPanel.Visible = false;

		SelectGame(_gamePickerButton.GetSelectableItem());
	}


	// Adds available and local mods to mod list
	private void AddMods(string gameId)
	{
		if (!_selectedSourceMods.ContainsKey(gameId) && !_installedMods.ContainsKey(gameId))
		{
			GD.Print("Cannot find mods for:" + gameId);
			_loadingPanel.Visible = false;
			return;
		}

		// Adds the available and installed mods
		if (_installedMods.TryGetValue(gameId, out var installedMods))
		{
			foreach (var mod in installedMods)
			{
				_modList.AddItem($@"  {mod.ModName} || Supports:{string.Join(", ", mod.CompatibleVersions)}  ",
					icon: _installedIcon);
			}
		}

		if (_selectedSourceMods.TryGetValue(gameId, out var selectedSourceMods))
		{
			foreach (var mod in selectedSourceMods)
			{
				_modList.AddItem($@"  {mod.ModName} || Supports:{string.Join(", ", mod.CompatibleVersions)}  ");
			}
		}

		_loadingPanel.Visible = false;
	}


	private void GetTitles(long result, long responseCode, string[] headers, byte[] body)
	{
		string[]
			gamesList = Encoding.UTF8.GetString(body).Split("<tr>"); // Splits the list into the beginnings of each game
		var gameList =
			gamesList.ToList(); // Converted to list so first and second item (headers and example text at top) can be removed

		if (gameList.Count < 2)
		{
			_tools.ErrorPopup("cannot retrieve titles");
			return;
		}

		gameList.RemoveRange(0, 2);

		foreach (string game in gameList)
		{
			// Removes the <td> and </td> html from our script for cleaning along with the special TM character otherwise the mod sites won't recognize the title.
			var gameCleaned = game.Replace("<td>", "").Replace("</td>", "").Replace("™", "");
			// Splits at every new line
			var gameSplit = gameCleaned.Split("\n");

			if (gameSplit.Length < 2)
			{
				_tools.ErrorPopup("unable to parse titles list, check connection and try again later.");
				_loadingPanel.Visible = false;
				return;
			}

			// Adds the game to our title list with type Dictionary(string ID, string Title, string modID)
			_titles[gameSplit[1]] = gameSplit[2];
		}
	}


	private async void UpdateAll()
	{
		var confirm = await _tools.ConfirmationPopup("Update all mods?");
		if (confirm == false)
		{
			return;
		}

		foreach (var installedGame in _installedGames)
		{
			// Mods list is temporarily duplicated to avoid issues with indexing when removing and re-adding the mods during update.
			foreach (var mod in new List<Mod>(_installedMods[installedGame.Key]))
			{
				if (mod.ModUrl != null)
				{
					var modUpdated = await UpdateMod(installedGame.Key, mod, true);
					if (modUpdated != true)
					{
						_tools.ErrorPopup($@"failed to update:{mod.ModName}");
						_loadingPanel.Visible = false;
						return;
					}
				}
			}
		}

		SelectGame(_gamePickerButton.Selected);
	}


	private async Task<bool> UpdateMod(string gameId, Mod mod, bool noConfirmation = false)
	{
		if (!noConfirmation)
		{
			var confirm = await _tools.ConfirmationPopup($@"Update {mod.ModName}?");
			if (confirm == false)
			{
				return false;
			}
		}

		try
		{
			var removedMod = await DeleteMod(gameId, mod, _selectedSource, (int)Sources.All);
			if (removedMod != true)
			{
				return false;
			}

			await InstallMod(gameId, mod);
		}
		catch (Exception updateError)
		{
			_tools.ErrorPopup($@"failed to update mod:{updateError}");
			throw;
		}

		SelectGame(_gamePickerButton.Selected);
		return true;
	}


	private void SelectGame(int gameIndex)
	{
		// Gets the keys we can equate as an List
		_currentGameId = GetGameIdFromValue(_gamePickerButton.GetItemText(gameIndex).Trim(), _installedGames);
		// Clears old mods from our list
		_modList.Clear();
		AddMods(_currentGameId);
	}


	static string GetGameIdFromValue(string value, Dictionary<string, Game> installedGames)
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


	private void SaveInstalledMods()
	{
		var serializedMods = JsonSerializer.Serialize(_installedMods);
		File.WriteAllText(_installedModsPath, serializedMods);
	}


	private void DisableInteraction(bool interactionDisabled = true)
	{
		for (int itemIndex = 0; itemIndex < _modList.ItemCount; itemIndex++)
		{
			_modList.SetItemDisabled(itemIndex, interactionDisabled);
		}

		_gamePickerButton.Disabled = interactionDisabled;
		_sourcePickerButton.Disabled = interactionDisabled;
		_modLocationButton.Disabled = interactionDisabled;
		_refreshButton.Disabled = interactionDisabled;
		_updateAllButton.Disabled = interactionDisabled;
		_updateSelectedButton.Disabled = interactionDisabled;
		if (_selectedSource is (int)Sources.Banana or (int)Sources.All)
		{
			_loadMoreButton.Disabled = interactionDisabled;
		}
	}
	
	
	// Helper functions
	private async Task InstallMod(string gameId, Mod mod)
	{
		UpdateManagers();
		switch (_selectedSource)
		{
			case (int)Sources.TotkHolo:
				await _totkHoloManager.InstallMod(gameId, mod);
				break;
			default:
				await _standardModManager.InstallMod(gameId, mod);
				_downloadBar.Value = 100;
				break;
		}
	}


	private async Task<bool> DeleteMod(string gameId, Mod mod, int source, int sourcesAll, bool noConfirmation = false)
	{
		UpdateManagers();
		switch (_selectedSource)
		{
			case (int)Sources.TotkHolo:
				break;
			default:
				return await _standardModManager.DeleteMod(gameId, mod, source, sourcesAll, noConfirmation);
		}

		return false;
	}
	
	
	private void UpdateManagers()
	{
		_standardModManager = new()
		{
			InstalledMods = _installedMods,
			SelectedSourceMods = _selectedSourceMods,
			InstalledModsPath = _installedModsPath,
			DownloadRequester = _downloadRequester,
			DownloadUpdateTimer = _downloadUpdateTimer,
			ToolsNode = _tools,
			LoadingPanel = _loadingPanel

		};

		_totkHoloManager = new()
		{
			ToolsNode = _tools,
			InstalledMods = _installedMods,
			SelectedSourceMods = _selectedSourceMods
		};
	}
	
	
	// Signal functions
	private void SearchUpdated(string newSearch)
	{
		if (_selectedSourceMods.TryGetValue(_currentGameId, out var sourceMods))
		{
			var localSourceMods = new List<Mod>(sourceMods);
			var searchQuery = newSearch.ToLower().Trim();
			_modList.Clear();

			if (_installedMods.ContainsKey(_currentGameId))
			{
				localSourceMods.InsertRange(0, _installedMods[_currentGameId]);
			}

			foreach (var mod in localSourceMods)
			{
				if (mod.ModName.ToLower().Trim().Contains(searchQuery) || searchQuery == "")
				{
					if (mod.InstalledPath != null)
					{
						_modList.AddItem($@"  {mod.ModName} || Supports:{string.Join(", ", mod.CompatibleVersions)}  ",
							icon: _installedIcon);
					}
					else
					{
						_modList.AddItem($@"  {mod.ModName} || Supports:{string.Join(", ", mod.CompatibleVersions)}  ");
					}
				}
			}
		}
	}


	private async void ModClicked(int modIndex)
	{
		// If the mod is found in the installed mods list removes it
		if (_installedMods.TryGetValue(_currentGameId, out var installedMods))
		{
			foreach (var mod in installedMods)
			{
				if (_modList.GetItemText(modIndex).Split(TitleSplitter)[0].Trim() == (mod.ModName))
				{
					DisableInteraction();
					await DeleteMod(_currentGameId, mod, _selectedSource, (int)Sources.All);
					DisableInteraction(false);
					
					//Used to update UI with installed icon	
					SelectGame(_gamePickerButton.Selected);
					return;
				}
			}
		}

		// Installs the mod from the online source
		if (_selectedSourceMods.TryGetValue(_currentGameId, out var selectedSourceMods))
		{
			foreach (var mod in selectedSourceMods)
			{
				if (_modList.GetItemText(modIndex).Split(TitleSplitter)[0].Trim() == (mod.ModName))
				{
					DisableInteraction();
					await InstallMod(_currentGameId, mod);
					DisableInteraction(false);

					//Used to update UI with installed icon	
					SelectGame(_gamePickerButton.Selected);
					return;
				}
			}
		}
	}


	// Refreshes the mods with the newly selected source
	private void SelectSource(int sourceIndex)
	{
		_selectedSource = _sourceNames.IndexOf(_sourcePickerButton.GetItemText(sourceIndex).Trim());
		if (_selectedSource == -1)
		{
			_tools.ErrorPopup("source not found, please file a bug report. Defaulting back to official");
			_sourcePickerButton.Select(0);
			return;
		}

		switch (_selectedSource)
		{
			case (int)Sources.Banana:
				_loadMoreButton.Disabled = false;
				break;
			case (int)Sources.All:
				_loadMoreButton.Disabled = false;
				break;
			default:
				_loadMoreButton.Disabled = true;
				break;
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

			if (_modList.GetItemText(selectedMods[0]).Split(TitleSplitter)[0].Trim() == (mod.ModName))
			{
				_loadingPanel.Visible = true;
				if (mod.ModUrl != null)
				{
					await UpdateMod(_currentGameId, mod);
				}

				// Used to update UI with installed icon
				SelectGame(_gamePickerButton.Selected);
				_loadingPanel.Visible = false;
				return;
			}
		}
	}


	private void ModLocationPressed()
	{
		var modLocationInput = _tools.OpenFileChooser(Globals.Instance.Settings.ModsLocation);
		if (modLocationInput != null)
		{
			Globals.Instance.Settings.ModsLocation = modLocationInput;
		}

		if (Globals.Instance.Settings.ModsLocation != null)
		{
			_modLocationButton.Text =
				Globals.Instance.Settings.ModsLocation.PadLeft(
					Globals.Instance.Settings.ModsLocation.Length + _selectionPaddingLeft, ' ');
		}

		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
	}


	private void RefreshPressed()
	{
		Refresh(_selectedSource);
	}

	private void UpdateDownloadProgress()
	{
		_downloadBar.Value = (float)_downloadRequester.GetDownloadedBytes() / _downloadRequester.GetBodySize() * 100;
	}
}

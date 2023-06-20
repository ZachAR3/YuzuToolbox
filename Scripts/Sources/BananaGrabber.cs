using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Array = Godot.Collections.Array;
using HttpClient = System.Net.Http.HttpClient;

public class BananaGrabber
{
	private readonly HttpClient _httpClient = new HttpClient();
	
	
	public async Task<Dictionary<string, List<Mod>>> GetAvailableMods(Dictionary<string, List<Mod>> modList, Dictionary<string, Game> installedGames, string gameId, int sourceId)
	{
		modList[gameId] = new List<Mod>();
		
		int bananaGameId = GetGameModId(installedGames[gameId].GameName);
		if (bananaGameId == -1)
		{
			return null;
		}
		
		int currentPage = 1;
		string gameModsSource = _httpClient
			.GetAsync($@"https://gamebanana.com/apiv11/Game/{bananaGameId}/Subfeed?_nPage={currentPage}").Result
			.Content
			.ReadAsStringAsync().Result;
		var jsonMods = JObject.Parse(gameModsSource);


		foreach (var mod in jsonMods["_aRecords"])
		{
			string modPage = _httpClient.GetAsync($@"https://gamebanana.com/apiv11/Mod/{mod["_idRow"]}/Files").Result.Content.ReadAsStringAsync().Result;
			var modPageContent = JToken.Parse(modPage);
			string downloadUrl = modPageContent[0]["_sDownloadUrl"].ToString();
			
			// If there is an available compatible version sets it as that, otherwises sets it as NA
			List<string> compatibleVersions = mod["_sVersion"] == null
				? new List<string>() { "NA" }
				: new List<string>() { mod["_sVersion"].ToString() };
			
			modList[gameId].Add(new Mod
			{
				ModName = mod["_sName"].ToString(), 
				ModUrl = downloadUrl, 
				CompatibleVersions = compatibleVersions, 
				Source = sourceId, 
				InstalledPath = null
			});
		}

		return modList;
	}
	
	 
	private int GetGameModId(string gameName)
	{
		// Searches for the game ID using the name from banana mods
		string searchContent = _httpClient.GetAsync("https://gamebanana.com/apiv11/Util/Game/NameMatch?_sName=" + gameName).Result.Content.ReadAsStringAsync().Result;
		var jsonContent = JObject.Parse($@"{searchContent}");
		var modId = jsonContent["_aRecords"]?[0]?["_idRow"]!.ToString();
		
		// If the return is null replace with our version (-1)
		int returnValue = modId == null ? -1 : int.Parse(modId);
		return returnValue;
	}
}
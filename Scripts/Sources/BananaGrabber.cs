using Godot;
using System;
using System.Threading.Tasks;
using Godot.Collections;
using Newtonsoft.Json.Linq;
using HttpClient = System.Net.Http.HttpClient;

public partial class BananaGrabber : Node
{
	private readonly HttpClient _httpClient = new HttpClient();
	
	
	public async Task<Dictionary<string, Array<Mod>>> GetAvailableMods(Dictionary<string, Array<Mod>> modList,
		Dictionary<string, Game> installedGames, string gameId, int sourceId)
	{
		modList[gameId] = new Array<Mod>();
		
		int bananaGameId = GetGameModId(installedGames[gameId].GameName);
		if (bananaGameId == -1)
		{
			GD.Print("Cannot find game id for selected game on banana mods");
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
			modList[gameId].Add(new Mod(mod["_sName"].ToString(), downloadUrl, new Array<string>() {"N/A"}, sourceId,
				null));
		}

		return modList;
	}
	
	
	private int GetGameModId(string gameName)
	{
		// Searches for the game ID using the name from banana mods
		string searchContent = _httpClient.GetAsync("https://gamebanana.com/apiv11/Util/Game/NameMatch?_sName=" + gameName).Result.Content.ReadAsStringAsync().Result;
		var jsonContent = JObject.Parse($@"{searchContent}");
		var returnValue = jsonContent["_aRecords"]?[0]?["_idRow"]!.ToString().ToInt();
		// If the return is null replace with our version (-1)
		returnValue ??= -1;
		return (int)returnValue;
	}
}

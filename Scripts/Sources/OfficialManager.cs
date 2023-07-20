using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using HtmlAgilityPack;
using HttpClient = System.Net.Http.HttpClient;

public class OfficialManager
{
    private const string Quote = "\"";
    private readonly HttpClient _httpClient = new();


    public async Task<Dictionary<string, List<Mod>>> GetAvailableMods(Dictionary<string, List<Mod>> modList, Dictionary<string, Game> installedGames, string gameId, int sourceId)
    {
        if (!modList.ContainsKey(gameId))
        {
            modList[gameId] = new List<Mod>();
        }
        
        var modsSourcePageResponse = await _httpClient.GetAsync("https://github.com/yuzu-emu/yuzu/wiki/Switch-Mods");
        var modsSourcePage = new HtmlDocument();
        modsSourcePage.LoadHtml(await modsSourcePageResponse.Content.ReadAsStringAsync());
        
        // List of elements, which MOSTLY contain mods (not all)
        var mods = modsSourcePage.DocumentNode.SelectNodes(
            $@"//h3[contains(., {Quote}{installedGames[gameId].GameName}{Quote})]/following::table[1]//td//a");
       
        // Throws an exception if the mods list is null
        _ = mods ?? throw new ArgumentException($@"No mods available");

        // Starts at an offset to account for the first example item in the site list
        int titleIndex = 1;
        // Loops through each element in the list of mostly mods
        foreach (var mod in mods)
        {
            string downloadUrl = mod.GetAttributeValue("href", null).Trim();
            string modName = mod.InnerText;
            ;
            // Checks if the item is a mod by ensuring it ends with a usable extension
            if (downloadUrl.EndsWith(".rar") || downloadUrl.EndsWith(".zip") || downloadUrl.EndsWith(".7z"))
            {
                var modVersions = modsSourcePage.DocumentNode.SelectNodes(
                    $@"//h3[contains(., {Quote}{installedGames[gameId].GameName}{Quote})]/following::table[1]//tr[{titleIndex}]/td//code");
                List<string> versions = new List<string>();
                foreach (var version in modVersions)
                {
                    versions.Add(version.InnerText);
                }
                titleIndex++;
                modList[gameId].Add(new Mod 
                {
                    ModName = modName, 
                    ModUrl = downloadUrl, 
                    CompatibleVersions = versions, 
                    Source = sourceId, 
                    InstalledPath = null
                });
            }
        }

        return modList;
    }
}

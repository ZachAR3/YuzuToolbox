using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using Godot.Collections;
using HtmlAgilityPack;

public partial class OfficialManager : Node
{
    private const string Quote = "\"";
    
    
    public async Task<Dictionary<string, Array<Mod>>> GetAvailableMods(Dictionary<string, Array<Mod>> officialModList, Dictionary<string, Game> installedGames, string gameId, int sourceId)
    {
        officialModList[gameId] = new Array<Mod>();

        var htmlWeb = new HtmlWeb();
        var modsSourcePage = htmlWeb.Load("https://github.com/yuzu-emu/yuzu/wiki/Switch-Mods");
        // List of elements, which MOSTLY contain mods (not all)
        var mods = modsSourcePage.DocumentNode.SelectNodes(
            $@"//h3[contains(., {Quote}{installedGames[gameId].GameName}{Quote})]/following::table[1]//td//a");
       
        // Throws an exception if the mods list is null
        _ = mods ?? throw new ArgumentException("Failed to retrieve mods for", gameId);

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
                Array<string> versions = new Array<string>();
                foreach (var version in modVersions)
                {
                    versions.Add(version.InnerText);
                }
                titleIndex++;
                officialModList[gameId].Add(new Mod(modName, downloadUrl, versions, sourceId));
            }
        }

        return officialModList;
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Mono.Posix;
using FileAccess = Godot.FileAccess;
using HttpClient = System.Net.Http.HttpClient;

namespace YuzuEAUpdateManager.Scripts.Sources;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;


public class TotkHoloManager
{
    private const string RepoOwner = "HolographicWings";
    private const string RepoName = "TOTK-Mods-collection";

    public Tools ToolsNode;
    public Dictionary<string, List<Mod>> InstalledMods;
    public Dictionary<string, List<Mod>> SelectedSourceMods;

    private GitHubClient _gitHubClient;
    private readonly HttpClient _httpClient = new();


    public async Task<Dictionary<string, List<Mod>>> GetAvailableMods(Dictionary<string, List<Mod>> modList,
        string gameId, int sourceId, bool getCompatibleVersions = false)
    {
        _gitHubClient = Globals.Instance.LocalGithubClient;
        if (!modList.ContainsKey(gameId))
        {
            modList[gameId] = new List<Mod>();
        }
        
        var contentPath = "Mods";
        var modContents = await _gitHubClient.Repository.Content.GetAllContents(RepoOwner, RepoName, contentPath);

        // Throws an exception if the mods list is null
        _ = modContents ?? throw new ArgumentException("Failed to retrieve mods for", gameId);


        foreach (var modType in modContents)
        {
            if (modType.Type == ContentType.Dir)
            {
                var mods = await _gitHubClient.Repository.Content.GetAllContents(RepoOwner, RepoName, modType.Path);
                foreach (var mod in mods)
                {
                    if (mod.Type == ContentType.Dir)
                    {
                        // Gets the compatible versions
                        var compatibleVersions = getCompatibleVersions
                            ? await GetCompatibleVersions(mod)
                            : new List<string> { "NA" };

                        var modToAdd = new Mod
                        {
                            ModName = mod.Name,
                            ModUrl = mod.Path,
                            CompatibleVersions = compatibleVersions,
                            Source = sourceId
                        };
                        modList[gameId].Add(modToAdd);
                    }
                }
            }
        }

        return modList;
    }


    public async Task InstallMod(string gameId, Mod mod)
    {
        try
        {
            await Task.Run(async() =>
            {
                string installPath = Path
                    .Join(Globals.Instance.Settings.ModsLocation, gameId, $@"Managed{mod.ModName.Replace(":", ".")}");

                var exception = await ToolsNode.DownloadFolder(RepoOwner, RepoName, mod.ModUrl, installPath);
                if (exception != null)
                {
                    ToolsNode.ErrorPopup($@"failed to download: {mod.ModName}. Exception: {exception}");
                    return;
                }
                
                mod.InstalledPath = installPath;
                InstalledMods[gameId] = !InstalledMods.ContainsKey(gameId)
                    ? new List<Mod>()
                    : InstalledMods[gameId];

                InstalledMods[gameId].Add(mod);
                SelectedSourceMods[gameId].Remove(mod);
            });
        }
        catch (Exception installError)
        {
            ToolsNode.ErrorPopup($@"failed to install mod:{installError}");
        }
    }
    
    
    // Helper functions
    private async Task<List<string>> GetCompatibleVersions(RepositoryContent mod)
    {
        var modFiles = await _gitHubClient.Repository.Content.GetAllContents(RepoOwner, RepoName, mod.Path);
        List<string> compatibleVersions = new();
                       
        foreach (var file in modFiles)
        {
            if (file.Type == ContentType.File && file.Name == "Compatible versions.txt")
            {
                var compatibleVersionsResponse = await _httpClient.GetAsync(file.DownloadUrl);
                var compatibleVersionsString = await compatibleVersionsResponse.Content.ReadAsStringAsync();
                var compatibleVersionsList = compatibleVersionsString.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1) // Skip the first line (mod type)
                    .ToList();

                // If it supports all versions, replace the list with "All"
                if (compatibleVersionsList.TrueForAll(versionText => versionText.Contains("Yes")))
                {
                    compatibleVersionsList = new List<string>() { "All" };
                }
                else
                {
                    compatibleVersionsList = compatibleVersionsList
                        .Select(line => line.Split(":").First().Trim())
                        .ToList();
                }

                compatibleVersions.AddRange(compatibleVersionsList);
                return compatibleVersions;
            }
        }

        GD.Print("No compatible versions found returning NA");
        return new List<string> { "NA" };
    }
}
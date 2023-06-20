using System;
using System.Collections.Generic;

public class Mod
{
    // Mod name, List<mod url, mod version
    public string ModName { get; set; }
    public string ModUrl { get; set; }
    public List<string> CompatibleVersions { get; set; }
    public int Source { get; set; }
    public string InstalledPath { get; set; }
    
    
    // public Mod(string modName, string modUrl, List<string> compatibleVersions, int source, string installedPath)
    // {
    //     ModName = modName;
    //     ModUrl = modUrl;
    //     CompatibleVersions = compatibleVersions;
    //     Source = source;
    //     InstalledPath = installedPath;
    // }
}

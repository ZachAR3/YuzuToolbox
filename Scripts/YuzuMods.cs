using Godot;
using System;
using Godot.Collections;

public partial class YuzuMod : Node
{
    // Mod name, List<mod url, mod version
    public string ModName;
    public string ModUrl;
    public Array<string> CompatibleVersions;
    public bool IsInstalled;
    public string CurrentVersion;
    
    
    public YuzuMod(string modName, string modUrl, Array<string> compatibleVersions, string currentVersion, bool isInstalled)
    {
        ModName = modName;
        ModUrl = modUrl;
        CompatibleVersions = compatibleVersions;
        CurrentVersion = currentVersion;
        IsInstalled = isInstalled;
    }
}

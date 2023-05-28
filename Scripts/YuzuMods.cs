using Godot;
using System;

public partial class YuzuMod : Node
{
    // Mod name, List<mod url, mod version
    public string ModName;
    public string ModUrl;
    public float ModVersion;
    public bool IsInstalled;
    
    
    public YuzuMod(string modName, string modUrl, float modVersion, bool isInstalled)
    {
        ModName = modName;
        ModUrl = modUrl;
        ModVersion = modVersion;
        IsInstalled = isInstalled;
    }
}

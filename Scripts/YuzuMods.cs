using Godot;
using System;

public partial class YuzuMod : Node
{
    // Mod name, List<mod url, mod version
    public string ModUrl;
    public float ModVersion;
    
    
    public YuzuMod(string modUrl, float modVersion)
    {
        ModUrl = modUrl;
        ModVersion = modVersion;
    }
}

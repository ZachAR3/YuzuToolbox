using Godot;
using System;
using Godot.Collections;

public partial class Mod : Node
{
    // Mod name, List<mod url, mod version
    public string ModName;
    public string ModUrl;
    public Array<string> CompatibleVersions;
    public int Source;
    
    
    public Mod(string modName, string modUrl, Array<string> compatibleVersions, int source)
    {
        ModName = modName;
        ModUrl = modUrl;
        CompatibleVersions = compatibleVersions;
        Source = source;
    }
}

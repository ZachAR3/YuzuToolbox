using System.Collections.Generic;
using Godot;

namespace YuzuEAUpdateManager.Scripts.Sources;

public partial class TOTKHoloGrabber : Node
{
    // Mod name, List<mod url, mod version
    public string ModName { get; set; }
    public string ModUrl;
    public List<string> CompatibleVersions;
    public int Source;
    public string InstalledPath;
}
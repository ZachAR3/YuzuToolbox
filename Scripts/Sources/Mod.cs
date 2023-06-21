using System;
using System.Collections.Generic;

public class Mod
{
    public string ModName { get; set; }
    public string ModUrl { get; set; }
    public List<string> CompatibleVersions { get; set; } = new() { "NA" };
    public int Source { get; set; } = -1;
    public string InstalledPath { get; set; }
    
}

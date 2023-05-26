using Godot;
using System;
using System.Collections.Generic;

public partial class SettingsResource : Resource
{
	[Export()] public String SaveDirectory = "/";
	[Export()] public String ShadersLocation = "";
	[Export()] public String FromSaveDirectory = "";
	[Export()] public String ToSaveDirectory = "/";
	[Export()] public int InstalledVersion = -1;
	[Export()] public bool LightModeEnabled = false;
	[Export()] public bool Muted = true;
	[Export()] public String AppDataPath = "";
	[Export()] public Godot.Collections.Dictionary<string, string> InstalledTitles = new Godot.Collections.Dictionary<string, string>();
	[Export()] public Godot.Collections.Dictionary<string, string> Titles = new Godot.Collections.Dictionary<string, string>();
}

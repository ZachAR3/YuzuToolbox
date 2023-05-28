using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot.Collections;

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
	// Game id, game name
	[Export()] public Godot.Collections.Dictionary<string, string> Titles = new Godot.Collections.Dictionary<string, string>();
	//[Export()] public Godot.Collections.Dictionary<string, string> InstalledTitles = new Godot.Collections.Dictionary<string, string>();
	// Game id, mod names array
	//[Export()] public Godot.Collections.Dictionary<string, Array<string>> InstalledMods = new Godot.Collections.Dictionary<string, Array<string>>();
	[Export()] public Godot.Collections.Dictionary<string, Game> InstalledGames = new Godot.Collections.Dictionary<string, Game>();
}

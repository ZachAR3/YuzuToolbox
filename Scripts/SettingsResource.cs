using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot.Collections;

public partial class SettingsResource : Resource
{
	[Export()] public string SaveDirectory;
	[Export()] public string ShadersLocation;
	[Export()] public string FromSaveDirectory;
	[Export()] public string ToSaveDirectory;
	[Export()] public string ModsLocation;
	[Export()] public int InstalledVersion = -1;
	[Export()] public bool LightModeEnabled;
	[Export()] public bool Muted = true;
	[Export()] public string AppDataPath;
	[Export()] public string GithubApiToken;
}

using Godot;
using System;

public partial class SettingsResource : Resource
{
	[Export()] public String SaveDirectory = "/";
	[Export()] public String ShadersLocation = "";
	[Export()] public String FromSaveDirectory = "";
	[Export()] public String ToSaveDirectory = "/";
	[Export()] public int InstalledVersion = -1;
	[Export()] public bool LightModeEnabled = false;
	[Export()] public bool Muted = true;
}

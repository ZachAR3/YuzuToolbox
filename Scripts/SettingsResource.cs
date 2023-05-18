using Godot;
using System;

public partial class SettingsResource : Resource
{
	[Export()] public String SaveDirectory = "/";
	[Export()] public int InstalledVersion = -1;
	[Export()] public bool LightModeEnabled = false;
}

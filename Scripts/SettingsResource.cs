using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot.Collections;

public partial class SettingsResource : Resource
{
	[Export()] public String SaveDirectory = "/";
	[Export()] public String ShadersLocation = null;
	[Export()] public String FromSaveDirectory = null;
	[Export()] public String ToSaveDirectory = "/";
	[Export()] public String ModsLocation = null;
	[Export()] public int InstalledVersion = -1;
	[Export()] public bool LightModeEnabled = false;
	[Export()] public bool Muted = true;
	[Export()] public String AppDataPath = null;
}

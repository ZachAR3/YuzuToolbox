using Godot;
using Godot.Collections;
using YuzuToolbox.Scripts.Modes;


public partial class SettingsResource : Resource
{
	[Export] public string SaveDirectory;
	[Export] public string ExecutableName = "yuzu";
	[Export] public string ExecutablePath;
	[Export] public string ShadersLocation;
	[Export] public string FromSaveDirectory;
	[Export] public string ToSaveDirectory;
	[Export] public string ModsLocation;
	[Export] public int InstalledVersion = -1;
	[Export] public bool LightModeEnabled;
	public Mode AppMode; // Why is this in export? I removed it for now, else the program can't be compiled.
	[Export] public bool Muted = true;
	[Export] public string AppDataPath;
	[Export] public string GithubApiToken;
	[Export] public bool GetCompatibleVersions;
	[Export] public int DisplayMode;
	[Export] public bool LauncherMode;
}

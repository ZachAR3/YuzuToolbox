using Godot;
using System;

public partial class Globals : Node
{
	private static Globals _instance;

	public static Globals Instance => _instance;
	
	public ResourceSaveManager SaveManager = new ResourceSaveManager();
	public SettingsResource Settings = new SettingsResource();

	public override void _Ready()
	{
		SaveManager.Version = 2f;
		Settings = SaveManager.GetSettings();
		SetDefaultPaths();
		
		_instance = this;

		GetTree().CallDeferred("call_group", "Initiate", "Initiate");
	}

	public void SetDefaultPaths()
	{
		// Sets app data path default for first startup
		if (string.IsNullOrEmpty(Settings.AppDataPath))
		{
			if (OS.GetName() == "Linux")
			{
				Settings.AppDataPath =
					$@"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)}/yuzu/";
			}
			else if (OS.GetName() == "Windows")
			{
				Settings.AppDataPath =
					$@"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData)}\yuzu\";
			}
		}
		
		// Sets shaders location default for first startup
		if (string.IsNullOrEmpty(Settings.ShadersLocation))
		{
			Settings.ShadersLocation = $@"{Settings.AppDataPath}shader";
		}
		
		if (string.IsNullOrEmpty(Settings.ModsLocation))
		{
			Settings.ModsLocation = $@"{Settings.AppDataPath}load";
		}

		if (string.IsNullOrEmpty(Settings.FromSaveDirectory))
		{
			Settings.FromSaveDirectory = OS.GetName() == "Linux"
				? $@"{Settings.AppDataPath}nand/user/save"
				: $@"{Settings.AppDataPath}nand\user\save";
		}
		
		SaveManager.WriteSave(Settings);
	}

}

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
		SetFirstTimeStartupPaths();
		
		_instance = this;

		GetTree().CallDeferred("call_group", "Initiate", "Initiate");
	}

	public void SetFirstTimeStartupPaths()
	{
		// Sets app data path default for first startup
		if (Settings.AppDataPath == null)
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
			SaveManager.WriteSave(Settings);
		}
		
		// Sets shaders location default for first startup
		if (Settings.ShadersLocation == null)
		{
			Settings.ShadersLocation = $@"{Settings.AppDataPath}shader";
			SaveManager.WriteSave(Settings);
		}
	}

}

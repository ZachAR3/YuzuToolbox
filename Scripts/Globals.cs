using Godot;
using System;

public partial class Globals : Node
{
	private static Globals _instance;

	public static Globals Instance
	{
		get
		{
			return _instance;
		}
	}

	public ResourceSaveManager SaveManager = new ResourceSaveManager();
	public SettingsResource Settings = new SettingsResource();
	public override void _Ready()
	{
		Settings = SaveManager.GetSettings();
		_instance = this;
		
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

		GetTree().CallDeferred("call_group", "Initiate", "Initiate");
	}

}

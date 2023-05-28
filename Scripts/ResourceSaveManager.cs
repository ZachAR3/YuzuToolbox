using Godot;
using System;
using System.Threading;

public partial class ResourceSaveManager : Resource
{
	public const String SaveGameBasePath = "user://InternalSave";
	
	[Export()] public float Version;
	[Export()] public SettingsResource _settings;

	public void WriteSave(SettingsResource settings = null)
	{
		// If no settings are passed, use the current settings
		settings ??= _settings;
		ResourceSaver.Save(this, GetSavePath());
	}
	

	public static bool SaveExists()
	{
		return ResourceLoader.Exists(GetSavePath());
	}

	public static Resource LoadSaveGame()
	{
		if (SaveExists())
		{
			return ResourceLoader.Load(GetSavePath());
		}
		return null;
	}


	public SettingsResource GetSettings()
	{
		if (SaveExists())
		{
			var lastSave = (ResourceSaveManager)LoadSaveGame();
			
			// If the save is from a previous version, reset the settings
			if (lastSave.Version != Version)
			{
				GD.Print("Save version is different, resetting settings");
				_settings = new SettingsResource();
				WriteSave();
			}
			else
			{
				_settings = lastSave._settings;
			}
		}
		else
		{
			_settings = new SettingsResource();
			WriteSave();
		}

		return _settings;
	}
	

	static String GetSavePath()
	{
		var extension = OS.IsDebugBuild() ? ".tres" : ".res";
		return SaveGameBasePath + extension;
	}

}

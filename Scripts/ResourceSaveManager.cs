using Godot;
using System;

public partial class ResourceSaveManager : Resource
{
    private const String SaveGameBasePath = "user://InternalSave";
    
    [Export()] private float _version = 1f;
    [Export()] public SettingsResource _settings;

    public void WriteSave()
    {
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

    static String GetSavePath()
    {
        var extension = OS.IsDebugBuild() ? ".tres" : ".res";
        return SaveGameBasePath + extension;
    }

}

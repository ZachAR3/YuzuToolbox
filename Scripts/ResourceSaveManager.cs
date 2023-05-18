using Godot;
using System;

public partial class ResourceSaveManager : Resource
{
    public const String SaveGameBasePath = "user://InternalSave";
    
    [Export()] public float Version = 2f;
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

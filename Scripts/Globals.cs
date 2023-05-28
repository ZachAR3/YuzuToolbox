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
		GetTree().CallDeferred("call_group", "Page", "Initiate");
		//GetTree().CallGroup("Page", "Initiate");
	}

}

using Godot;
using System;
using System.IO;
using Octokit;

public partial class Globals : Node
{
	private static Globals _instance;

	public static Globals Instance => _instance;
	
	public ResourceSaveManager SaveManager = new();
	public SettingsResource Settings = new();
	public readonly GitHubClient LocalGithubClient = new(new ProductHeaderValue("PineappleEA-GUI"));

	public override void _Ready()
	{
		SaveManager.Version = 2.1f;
		Settings = SaveManager.GetSettings();
		SetDefaultPaths();
		if (!string.IsNullOrEmpty(Settings.GithubApiToken))
		{
			AuthenticateGithubClient();
		}
		
		_instance = this;

		GetTree().CallDeferred("call_group", "Initiate", "Initiate");
	}

	public void SetDefaultPaths()
	{
		// Sets app data path default for first startup
		if (string.IsNullOrEmpty(Settings.AppDataPath))
		{
			Settings.AppDataPath = OS.GetName() == "Linux"
				? Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
					"yuzu")
				: Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
					"yuzu");
		}
		
		// Sets shaders location default for first startup
		if (string.IsNullOrEmpty(Settings.ShadersLocation))
		{
			Settings.ShadersLocation = Path.Join(Settings.AppDataPath, "shader");
		}
		
		if (string.IsNullOrEmpty(Settings.ModsLocation))
		{
			Settings.ModsLocation = Path.Join(Settings.AppDataPath, "load");
		}

		if (string.IsNullOrEmpty(Settings.FromSaveDirectory))
		{
			Settings.FromSaveDirectory = Path.Join(Settings.AppDataPath, "nand", "user", "save");
		}
		
		SaveManager.WriteSave(Settings);
	}


	public void AuthenticateGithubClient()
	{
		LocalGithubClient.Credentials = new Credentials(Settings.GithubApiToken);
	}

}

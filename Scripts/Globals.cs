using Godot;
using System.IO;
using System.Linq;
using Octokit;
using YuzuToolbox.Scripts.Modes;
using Node = Godot.Node;

public partial class Globals : Node
{
	//private static Globals _instance;
	public static Globals Instance;

	public Mode AppMode = new ModeRyujinx();
	public ResourceSaveManager SaveManager = new();
	public SettingsResource Settings = new();
	
	// TODO replace header with finalized project name
	public readonly GitHubClient LocalGithubClient = new(new ProductHeaderValue("EmulationToolbox"));
	//public string DllsDirectory;

	public override void _Ready()
	{
		Instance = this;
		SaveManager.Version = 2.4f;
		Settings = SaveManager.GetSettings();
		SetDefaultPaths();
		if (!string.IsNullOrEmpty(Settings.GithubApiToken))
		{
			AuthenticateGithubClient();
		}
		
		// Get launch options and update settings accordingly.
		var launchOptions = OS.GetCmdlineArgs();
		Settings.LauncherMode = launchOptions.Contains("--launcher");
		SaveManager.WriteSave();

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

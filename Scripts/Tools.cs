using Godot;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using ContentType = Octokit.ContentType;
using HttpClient = System.Net.Http.HttpClient;

public partial class Tools : Node
{
	[Export()] private Control _errorConsoleContainer;
	[Export] private TextEdit _errorConsole;
	[Export] private RichTextLabel _errorNotifier;
	[Export] private PopupMenu _confirmationPopup;

	public static Tools Instance;
	
	// Internal variables
	private bool? _confirmationChoice;


	// Godot functions
	public override void _Ready()
	{
		Instance = this;
	}
	
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("OpenConsole"))
		{
			ToggleConsole();
		}
	}


	// General functions
	public void LaunchYuzu()
	{
		string executablePath = Globals.Instance.Settings.ExecutablePath;

		try
		{
			ProcessStartInfo yuzuProcessInfo = new(executablePath);

			Process yuzuProcess = Process.Start(yuzuProcessInfo);
			GetTree().Quit();
		}
		catch (Exception launchException)
		{
			AddError("Unable to launch Yuzu: " + launchException.Message);
		}	
	}
	
	
	private void ToggleConsole()
	{
		_errorConsoleContainer.Visible = !_errorConsoleContainer.Visible;
	}
	
	
	public async Task<bool?> ConfirmationPopup(string titleText = "Are you sure?")
	{
		// Checks if the confirmationPopup is already connected to the ConfirmationPressed signal, if not, connect it.
		if (!_confirmationPopup.IsConnected("index_pressed", new Callable(this, nameof(ConfirmationPressed))))
		{
			_confirmationPopup.Connect("index_pressed", new Callable(this, nameof(ConfirmationPressed)));
		}

		_confirmationPopup.Title = titleText;
		_confirmationPopup.PopupCentered();
		await ToSignal(_confirmationPopup, "index_pressed");
		return _confirmationChoice;
	}
	
	public bool ClearInstallationFolder(string saveDirectory)
	{
		bool clearedSuccessfully = DeleteDirectoryContents(saveDirectory);
		return clearedSuccessfully;
	}
	
	public static bool DeleteDirectoryContents(string directoryPath)
	{
		// Delete all files within the directory
		string[] files = Directory.GetFiles(directoryPath);
		foreach (string file in files)
		{
			File.Delete(file);
		}

		// Delete all subdirectories within the directory
		string[] directories = Directory.GetDirectories(directoryPath);
		foreach (string directory in directories)
		{
			// Recursively delete subdirectory contents
			DeleteDirectoryContents(directory); 
			Directory.Delete(directory);
		}

		return true;
	}
	
	
	public static void MoveFilesAndDirs(string sourceDirectory, string targetDirectory)
	{
		// Create the target directory if it doesn't exist
		if (!Directory.Exists(targetDirectory) && !string.IsNullOrEmpty(targetDirectory))
		{
			Directory.CreateDirectory(targetDirectory);
		}

		// Get all files and directories from the source directory
		string[] files = Directory.GetFiles(sourceDirectory);
		string[] directories = Directory.GetDirectories(sourceDirectory);

		// Move files to the target directory
		foreach (string file in files)
		{
			string fileName = Path.GetFileName(file);
			string targetPath = Path.Combine(targetDirectory, fileName);
			File.Move(file, targetPath);
		}

		// Move directories to the target directory
		foreach (string directory in directories)
		{
			string directoryName = Path.GetFileName(directory);
			string targetPath = Path.Combine(targetDirectory, directoryName);
			Directory.Move(directory, targetPath);
		}

		// Remove the source directory if it is empty
		if (Directory.GetFiles(sourceDirectory).Length == 0 && Directory.GetDirectories(sourceDirectory).Length == 0)
		{
			Directory.Delete(sourceDirectory);
		}
	}
	
	
	public void DuplicateDirectoryContents(string sourceDir, string destinationDir, bool overwriteFiles)
	{
		// Get all directories in the source directory
		string[] allDirectories = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);

		foreach (string dir in allDirectories)
		{
			string dirToCreate = dir.Replace(sourceDir, destinationDir);
			Directory.CreateDirectory(dirToCreate);
		}

		// Get all files in the source directory
		string[] allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

		foreach (string filePath in allFiles)
		{
			string newFilePath = filePath.Replace(sourceDir, destinationDir);
			File.Copy(filePath, newFilePath, overwriteFiles);
		}
	}
	
	
	public bool ClearShaders(string shaderLocation)
	{
		if (Directory.Exists(shaderLocation))
		{
			DeleteDirectoryContents(shaderLocation);
			return true;

		}
		return false;
	}
	
	
	public async void AddError(String error)
	{
		Callable.From(() =>
		{
			_errorConsole.Text += $"\n [{DateTime.Now:h:mm:ss}]	{error}";
			_errorNotifier.Visible = true;
		}).CallDeferred();
		
		await Task.Run(async () =>
		{
			await ToSignal(GetTree().CreateTimer(5), "timeout");
			_errorNotifier.SetThreadSafe("visible", false);
		});

	}


	// Signal functions
	private void ConfirmationPressed(long itemIndex)
	{
		_confirmationChoice = itemIndex == 0;
	}


	public async Task<Exception> DownloadFolder(string owner, string repo, string folderPath, string destinationPath)
	{
		try
		{
			HttpClient httpClient = new();
			var gitHubClient = Globals.Instance.LocalGithubClient;

			// Retrieve the repository content for the specified folder
			var contents = await gitHubClient.Repository.Content.GetAllContents(owner, repo, folderPath);

			// Create the destination folder
			Directory.CreateDirectory(destinationPath);

			// Download and copy each file in the folder
			foreach (var content in contents)
			{
				if (content.Type == ContentType.File)
				{
					var fileContent = await httpClient.GetStringAsync(content.DownloadUrl);
					var filePath = Path.Combine(destinationPath, content.Name);

					// Write the file content to disk
					await File.WriteAllTextAsync(filePath, fileContent);
				}
				else if (content.Type == ContentType.Dir)
				{
					var subFolderPath = Path.Combine(destinationPath, content.Name);

					// Recursively download and copy the contents of sub-folders
					await DownloadFolder(owner, repo, content.Path, subFolderPath);
				}
			}

			return null;
		}
		catch (Exception downloadException)
		{
			return downloadException;
		}
	}
}

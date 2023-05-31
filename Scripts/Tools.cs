using Godot;
using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Godot.Collections;
using Gtk;

public partial class Tools : Godot.Node
{
	// Internal variables
	private bool? _confirmationChoice;
	private FileChooserDialog _fileChooser;


	// General functions
	public async Task<bool?> ConfirmationPopup(PopupMenu confirmationPopup, string titleText = "Are you sure?")
	{
		// Checks if the confirmationPopup is already connected to the ConfirmationPressed signal, if not, connect it.
		if (!confirmationPopup.IsConnected("index_pressed", new Callable(this, nameof(ConfirmationPressed))))
		{
			confirmationPopup.Connect("index_pressed", new Callable(this, nameof(ConfirmationPressed)));
		}

		confirmationPopup.Title = titleText;
		confirmationPopup.PopupCentered();
		await ToSignal(confirmationPopup, "index_pressed");
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
		if (!Directory.Exists(targetDirectory))
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
			Tools.DeleteDirectoryContents(shaderLocation);
			return true;
			// Move to tools script when made
			// _clearShadersButton.Text = "Shaders cleared successfully!";
			// _clearShadersToolButton.Text = "Shaders cleared successfully!";
		}
		else
		{
			// Move to tools caller
			//ErrorPopup("failed to find shaders location");
			return false;
		}
			
	}
	
	
	public void ErrorPopup(String error, Godot.Label errorLabel, Popup errorPopup)
	{
		errorLabel.Text = $@"Error:{error}";
		errorPopup.Visible = true;
		errorPopup.InitialPosition = Godot.Window.WindowInitialPosition.Absolute;
		errorPopup.PopupCentered();
	}

	
	// File chooser functions
	public void OpenFileChooser(ref string returnObject, string startingDirectory, Godot.Label errorLabel, Popup errorPopup)
	{
		try
		{
			Application.Init();
		}
		catch (Exception gtkError)
		{
			ErrorPopup("opening GTK window failed. Ensure you have GTK runtime installed: " + gtkError, errorLabel, errorPopup);
			throw;
		}
		_fileChooser = new FileChooserDialog("Select a File", null, FileChooserAction.SelectFolder);

		// Add a "Cancel" button to the dialog
		_fileChooser.AddButton("Cancel", ResponseType.Cancel);

		// Add an "Open" button to the dialog
		_fileChooser.AddButton("Open", ResponseType.Ok);

		// Set the initial directory
		_fileChooser.SetCurrentFolder(startingDirectory);

		// Connect the response signal, I would like to directly pass in the return object, but this isn't possible 
		// in a lambda, so we create a temp value to hold it and then assign it to that value after.
		string tempReturnString = returnObject;

		_fileChooser.Response += (sender, args) => OnFileChooserResponse(sender, args, ref tempReturnString);
		_fileChooser.FocusOutEvent += (sender, args) => OnFileChooserResponse(sender, null, ref tempReturnString);

		// Show the dialog
		_fileChooser.Show();
		Application.Run();
		
		// Sets our original object back to be the returned temporary string.
		returnObject = tempReturnString;
	}


	// Signal functions
	private void ConfirmationPressed(long itemIndex)
	{
		_confirmationChoice = itemIndex == 0;
	}


	private void OnFileChooserResponse(object sender, ResponseArgs args, ref string returnObject)
	{
		// Ensures response args aren't null, and checks if it was given ok (means a file was selected)
		if (args is { ResponseId: ResponseType.Ok })
		{
			// The user selected a file
			returnObject = _fileChooser.File.Path;
		}

		// Clean up resources
		_fileChooser.Dispose();
		Application.Quit();
	}
}

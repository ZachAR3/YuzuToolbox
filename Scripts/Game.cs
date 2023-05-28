using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node
{
	public string GameName;
	// Mod name, mod item
	public Godot.Collections.Dictionary<string, YuzuMod> YuzuModsList;


	public Game (string gameName, Godot.Collections.Dictionary<string, YuzuMod> yuzuModsList)
	{
		GameName = gameName;
		YuzuModsList = yuzuModsList;
	}
	
}

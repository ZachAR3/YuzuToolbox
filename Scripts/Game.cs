using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node
{
	public string GameId;
	public string GameName;
	// Mod name, List<mod url, mod version
	public Dictionary<string, List<(string, float)>> Mods;
	
	
	public Game (string gameId, string gameName, Dictionary<string, List<(string, float)>> mods)
	{
		GameId = gameId;
		GameName = gameName;
		Mods = mods;
	}
	
}

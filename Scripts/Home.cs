using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Godot.Collections;
using Mono.Unix;
using ProgressBar = Godot.ProgressBar;
using WindowsShortcutFactory;

public partial class Home : Control
{
	[ExportGroup("App")]
	[Export()] private float _appVersion = 2.2f;
	[Export()] private float _saveManagerVersion = 1.9f;
	[Export()] private TextureRect _darkBg;
	[Export()] private TextureRect _lightBg;
	[Export()] private ColorRect _downloadWindowApp;
	[Export()] private AudioStreamPlayer _backgroundAudio;
	[Export()] private CheckButton _muteButton;
	[Export()] private CheckBox _enableLightTheme;
	[Export()] private Array<Theme> _themes;
	[Export()] private Array<StyleBoxLine> _themesSeparator;
	[Export()] private ColorRect _header;
	[Export()] private Label _headerLabel;
	[Export()] private Label _latestVersionLabel;

	[ExportGroup("ModManager")]
	[Export()] private ItemList _modList;


	// Internal variables
	private Theme _currentTheme;


	// Godot functions
	private void Initiate()
	{
		// Sets minimum window size to prevent text clipping and UI breaking at smaller scales.
		DisplayServer.WindowSetMinSize(new Vector2I(1024, 576));
		
		// Set save manager version
		Globals.Instance.SaveManager.Version = _saveManagerVersion;

		// Mute by default the music
		SetTheme(Globals.Instance.Settings.LightModeEnabled);
		
		// Signals
		Resized += WindowResized;
	}


	// Custom functions
	private void SetTheme(bool enableLight)
	{
		_lightBg.Visible = enableLight;
		_darkBg.Visible = !enableLight;
		_currentTheme = enableLight ? _themes[1] : _themes[0];
		_header.Color = enableLight ? new Godot.Color(0.74117648601532f, 0.76470589637756f, 0.78039216995239f) : new Godot.Color(0.16862745583057f, 0.1803921610117f, 0.18823529779911f);
		_downloadWindowApp.Color = enableLight ? new Godot.Color(0.74117648601532f, 0.76470589637756f, 0.78039216995239f) : new Godot.Color(0.16862745583057f, 0.1803921610117f, 0.18823529779911f);
		_enableLightTheme.ButtonPressed = enableLight;
		Globals.Instance.Settings.LightModeEnabled = enableLight;
		Globals.Instance.SaveManager.WriteSave(Globals.Instance.Settings);
		Theme = _currentTheme;
	}


	private void WindowResized()
	{
		float scaleRatio = (((float)GetWindow().Size.X / 1920) + ((float)GetWindow().Size.Y / 1080)) / 2;
		_modList.IconScale = scaleRatio;
		_headerLabel.AddThemeFontSizeOverride("font_size", (int)(scaleRatio * 76));
		_latestVersionLabel.AddThemeFontSizeOverride("font_size", (int)(scaleRatio * 32));
		_currentTheme.DefaultFontSize = Mathf.Clamp((int)(scaleRatio * 35), 20, 50);
	}


	// Signal functions
	private void ToggledMusicButton(bool musicEnabled)
	{
		AudioServer.SetBusMute(AudioServer.GetBusIndex("Master"), !musicEnabled);
	}
	
	
}

using System.Net;

namespace YuzuToolbox.Scripts.Modes;

public abstract class Mode
{
    public string Name;
    public string RepoName;
    public string RepoOwner;
    public string LatestDownloadUrl;
    public string DownloadBaseUrl;
    public string ReleasesUrl;
    public string WindowsFolderName;
    public string BaseString;
    public string InstallDirectory;
    public string ShaderDirectory;
    public string ModsDirectory;
    public string SaveDirectory;

    public abstract string GetDownloadLink(int version, string os);
}
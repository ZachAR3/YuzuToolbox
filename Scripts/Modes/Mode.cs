using System.Net;

namespace YuzuToolbox.Scripts.Modes;

public class Mode
{
    public string RepoName;
    public string RepoOwner;
    public string LatestDownloadUrl;
    public string DownloadBaseUrl;
    public string WindowsFolderName;
    public string BaseString;

    public string GetDownloadLink(int version, string os)
    {
        return "";
    }
}
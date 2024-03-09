namespace YuzuToolbox.Scripts.Modes;

public class ModeRyujinx : Mode
{
    public ModeRyujinx()
    {
        RepoName = "release-channel-master";
        RepoOwner = "Ryujinx";
        LatestDownloadUrl = "https://github.com/Ryujinx/release-channel-master/releases/latest";
        DownloadBaseUrl = "https://github.com/Ryujinx/release-channel-master/releases/download";
        WindowsFolderName = "yuzu-windows-msvc-early-access";
        BaseString = "ryujinx-";
    }

    
    public string GetDownloadLink(int version, string os)
    {
        string extension;
        string osPrefix;
        if (os == "Windows")
        {
            osPrefix = "win_x64";
            extension = "zip";
        }
        else if (os == "Linux")
        {
            osPrefix = "linux_x64";
            extension = "tar.gz";
        }
        else
        {
            osPrefix = "macos_universal.app";
            extension = "tar.gz";
        }
        
        return $@"{DownloadBaseUrl}/{version}/{BaseString}{version}-{osPrefix}.{extension}";
    }
}
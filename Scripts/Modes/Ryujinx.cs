using Godot;

namespace YuzuToolbox.Scripts.Modes;

public class ModeRyujinx : Mode
{
    public ModeRyujinx()
    {
        Name = "Ryujinx";
        RepoName = "release-channel-master";
        RepoOwner = "Ryujinx";
        LatestDownloadUrl = "https://github.com/Ryujinx/release-channel-master/releases/latest";
        DownloadBaseUrl = "https://github.com/Ryujinx/release-channel-master/releases/download";
        ReleasesUrl = "https://github.com/Ryujinx/release-channel-master/releases";
        WindowsFolderName = "yuzu-windows-msvc-early-access";
        BaseString = "ryujinx-";
    }


    public override string GetDownloadLink(int version, string os)
    {
        string versionString = Tools.FromInt(version);
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

        return $@"{DownloadBaseUrl}/{versionString}/{BaseString}{versionString}-{osPrefix}.{extension}";
    }
}
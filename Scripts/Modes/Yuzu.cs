using System;
using System.Runtime.CompilerServices;

namespace YuzuToolbox.Scripts.Modes;

public class ModeYuzu : Mode
{
    public ModeYuzu()
    {
        Name = "Yuzu";
        RepoName = "pineapple-src";
        RepoOwner = "pineappleEA";
        LatestDownloadUrl = "https://github.com/pineappleEA/pineapple-src/releases/latest";
        DownloadBaseUrl = "https://github.com/pineappleEA/pineapple-src/releases/download/EA-";
        ReleasesUrl = "https://github.com/pineappleEA/pineapple-src/releases";
        WindowsFolderName = "yuzu-windows-msvc-early-access";
        BaseString = "Yuzu-EA-";
    }
    
    
    public override string GetDownloadLink(int version, string os)
    {
        string extension = os == "Windows" ? "zip" : "tar.gz";

        return $@"{DownloadBaseUrl}{version}/{os}-{BaseString}{version}.{extension}";
    }
}
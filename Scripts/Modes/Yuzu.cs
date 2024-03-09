using System;
using System.Runtime.CompilerServices;

namespace YuzuToolbox.Scripts.Modes;

public class ModeYuzu : Mode
{
    public ModeYuzu()
    {
        RepoName = "pineapple-src";
        RepoOwner = "pineappleEA";
        LatestDownloadUrl = "https://github.com/pineappleEA/pineapple-src/releases/latest";
        DownloadBaseUrl = "https://github.com/pineappleEA/pineapple-src/releases/download/EA-";
        WindowsFolderName = "yuzu-windows-msvc-early-access";
        BaseString = "Yuzu-EA-";
    }
    
    
    public string GetDownloadLink(int version, string os)
    {
        string extension = os == "Windows" ? "zip" : "tar.gz";

        return $@"{DownloadBaseUrl}{version}/{os}-{BaseString}{version}.{extension}";
    }
}
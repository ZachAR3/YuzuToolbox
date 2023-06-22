# PineappleEA-GUI
A simple tool to download and update early access builds of yuzu from the [PineappleEA](https://github.com/pineappleEA/pineapple-src "PineappleEA") Github repo. This tool is currently supports 
* Cross platform (Windows and Linux)
* updating with overwrites of previous versions
* Simple management tools such as clearing shader caches / install directory
* Shortcut creation and automatic unpacking
* Basic backup tool (allows save directory to be duplicated into another directory and then restored when desired)
* Mod management features (downloading, installing and updating mods from a variety of sources; + ability to uninstall and detect manually installed mods)

# Dependencies
GTK runtime:
* Windows: https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer)
* Linux: Distro-dependent but should be included in your distros mono package. Or you can download it by itself e.g libgtk-3-0 on debian or gtk3 for Arch.


![](https://github.com/ZachAR3/PineappleEA-GUI/blob/main/DemoImages/DarkInstaller.png?raw=true)![](https://github.com/ZachAR3/PineappleEA-GUI/blob/main/DemoImages/DarkTools.png?raw=true)![](https://github.com/ZachAR3/PineappleEA-GUI/blob/main/DemoImages/DarkModManager.png?raw=true)
![](https://github.com/ZachAR3/PineappleEA-GUI/blob/main/DemoImages/LightInstaller.png?raw=true)![](https://github.com/ZachAR3/PineappleEA-GUI/blob/main/DemoImages/LightTools.png?raw=true)![](https://github.com/ZachAR3/PineappleEA-GUI/blob/main/DemoImages/LightModManager.png?raw=true)


# Resources:
Big thanks to the repo owners of:
* https://github.com/pilout/YuzuUpdater/blob/master/YuzuEAUpdater/
* https://github.com/amakvana/YuzuModDownloader/blob/main/source/YuzuModDownloader/classes/OfficialYuzuModDownloader.cs

I was able to use a lot of their code for the mod managing, sections which I found difficult (Especially figuring out the XPaths).


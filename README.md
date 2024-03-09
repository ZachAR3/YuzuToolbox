# Notice
This project is currently undergoing a large rework to add Ryujinx and MacOS support. Expect master branch to be broken, please download a release directly or from the Itch store page.

# YuzuToolbox
A simple tool to download and update early access builds of yuzu from the [PineappleEA](https://github.com/pineappleEA/pineapple-src "PineappleEA") Github repo. This tool is currently supports 
* Cross platform (Windows and Linux)
* updating with overwrites of previous versions
* Simple management tools such as clearing shader caches / install directory
* Shortcut creation and automatic unpacking
* Basic backup tool (allows save directory to be duplicated into another directory and then restored when desired)
* Mod management (downloading, installing and updating mods from a variety of sources; + ability to uninstall and detect manually installed mods)

# Donate
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/R5R4NFO8V)

# Installing
The recommended method of install is through Itch.io as it provoides updating features and easier launching: https://zachar3.itch.io/yuzutoolbox
However, if one would still like to install it without Itch one can simply go to [releases page](https://github.com/ZachAR3/YuzuToolbox/releases) and download the zip file for your os and then extract it into its own folder.

# Usage
Simply launch the program and set your install location, then click download and wait for it to finish. Optionally, you can launch the program with --launcher after initially installing Yuzu to automatically check for updates (update if available) and then launch Yuzu closing itself, The recommended method of using this feature is to enable "Auto Updates" with "Create Shortcut" upon installation so the created shortcut leads back to the launcher with this flag.

(WARNING:Most banana mods are meant for the nintendo switch and NOT Yuzu, meaning they will not work if installed. Please do not make issues regarding this)


![](https://github.com/ZachAR3/YuzuToolbox/blob/main/DemoImages/DarkInstaller.png?raw=true)
![](https://github.com/ZachAR3/YuzuToolbox/blob/main/DemoImages/DarkTools.png?raw=true)
![](https://github.com/ZachAR3/YuzuToolbox/blob/main/DemoImages/DarkModManager.png?raw=true)
![](https://github.com/ZachAR3/YuzuToolbox/blob/main/DemoImages/DarkSettings.png?raw=true)
![](https://github.com/ZachAR3/YuzuToolbox/blob/main/DemoImages/LightInstaller.png?raw=true)
![](https://github.com/ZachAR3/YuzuToolbox/blob/main/DemoImages/LightTools.png?raw=true)
![](https://github.com/ZachAR3/YuzuToolbox/blob/main/DemoImages/LightModManager.png?raw=true)
![](https://github.com/ZachAR3/YuzuToolbox/blob/main/DemoImages/LightSettings.png?raw=true)


# Resources:
Big thanks to the repo owners of:
* https://github.com/pilout/YuzuUpdater/blob/master/YuzuEAUpdater/
* https://github.com/amakvana/YuzuModDownloader/blob/main/source/YuzuModDownloader/classes/OfficialYuzuModDownloader.cs

I was able to use a lot of their code for the mod managing, sections which I found difficult (Especially figuring out the XPaths).


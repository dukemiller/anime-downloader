# anime-downloader
A program to download and manage a collection of currently airing anime shows. 

**Features**  
\- Manage and track basic information about shows.  
\- Search for and download any newly found episodes through a few downloading options.  
\- Support for downloading from specific subgroups, and setting a whitelist subgroups to download from.  
\- Set custom paths for where videos and torrent files download to and where watched files move to.  
\- A basic episode file management system for moving unwatched->watched or deleting.  
\- Create playlists with some file organization options.  
\- MyAnimeList synchronization to your anime list (*experimental*).  

**To be implemented**  
\- Downloading episode ranges (e.g. 12-13)  
\- Detecting and downloading two part episodes (e.g. Re: Zero 01a & 01b)  
\- Searching for any episode 00 (e.g. Tales of Zestira - 00)  

---

### Build & Run
**Requirements:**  [nuget.exe](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe) on PATH, Visual Studio 2015 and/or C# 6.0 Roslyn Compiler  
**Optional:** Devenv (Visual Studio 2015) on PATH  
```
git clone https://github.com/dukemiller/anime-downloader.git
cd anime-downloader
nuget install anime-downloader\packages.config -OutputDirectory packages
```
**Building with Devenv (CLI):** ```devenv anime-downloader.sln /Build```  
**Building with Visual Studio:**  Open (ctrl-shift-o) "anime-downloader.sln", Build Solution (ctrl-shift-b)

An "anime-downloader.exe" artifact will be created in the parent anime-downloader folder.

---

### Notes
\- This is my first larger sized application using WPF, based on very little knowledge of both C# and WPF. That is to say a lot of bad practices are probably in the code.  
\- Not tested on other platforms other than **Windows**, and im assuming that since it's WPF based that the support for Mono for Linux/Mac is pretty nonexistant.  
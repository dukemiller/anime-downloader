# anime-downloader
A program to download and manage a collection of currently airing anime 
shows. 

### Features

* Manage and track basic information about shows
* Search for and download any newly found episode through a few downloading options
* Support for downloading from only specific subgroups and only allowing a whitelist of subgroups to be downloaded from
* Set paths for where files download to, where watched files download to and torrent files
* Some relative simple management of managing episode files either moving through designated folders or deleting
* Simple playlist creation with a few options for how the files are organized
* **MyAnimeList** synchronization to your anime list*

&#42; Experimental

---

### Build and Run Instructions  

**Requirements**:
[nuget.exe](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe)
 on PATH 
Visual Studio 2015 and/or C# 6.0 Roslyn Compiler

**Shell**:  
``` 
git clone https://github.com/dukemiller/anime-downloader.git
cd anime-downloader\anime-downloader
nuget install packages.config
```
**Visual studio**:  
open (ctrl-shift-o) "anime-downloader.sln"  
start without debugging (ctrl-f5)

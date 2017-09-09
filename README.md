# anime-downloader

A program to download and manage a collection of currently airing anime shows. 

**\* [Find the download link for the latest version here.](https://github.com/dukemiller/anime-downloader/releases/latest)**  

### Demo 

![demo](http://i.imgur.com/rdk9JwK.gif)  

---

## Features 

- Manage and track basic information about downloaded shows (name, episode, status, rating, optional notes, etc).
- Discover shows airing now and add them to your list without having to search through external sites.
- Search for and download any newly found episodes, with a few custom options.
- Download filtering, like selecting to download from specific subgroups and setting a whitelist subgroups to download from.
- Set paths to where episodes and torrent files download to and where watched files move to.
- Simple file management system for moving unwatched<->watched, deleting or creating playlists (with a few helpful filtering options if needed).
- (*experimental*) MyAnimeList synchronization to your anime list. Sync your anime by going to the web view, logging in and pressing  sync. It will attempt to find MyAnimeList entries for anime on your list and any further changes on the details on the anime will be marked for synchronization. Any further syncs will update your list.

## Shortcuts

**Alt+[1-8]**: Change view.  
**Ctrl+X**: Close program.  
**Pressing enter** while focused on a selected input (textbox, radio, anime selection) will generally have the result of attempting to save the details on the page or do the main action (anime details, settings, download, web, misc, etc.).  
**Escape** will either clear out the selected input (textbox) or go back a view if in a sub-view (details of an anime).  
While on the anime details page unfocused or clicking on another element to remove focus, **arrow left/page up** will go to the previous entry on the list and **arrow right/page down** will go to the next entry on the list.  
While on the Anime tab and focused on the list: **Ctrl+F** will open the find bar, **Ctrl+C** will copy the selected names.  

## Usage notes

File indexing: On the occasion your torrent download is corrupted / you stop a file and remove it or you download the wrong series, make use of the simple tools in "Misc". Following the workflow of: Misc>re-index by last watched episode, DownloadOptions> download any missing between first and last, and DownloadOptions> download next found, will solve most issues related to incorrectly downloaded / missing episodes

A lot of options and selections have tooltips. If there's a selection that doesn't make a lot of sense or is unclear, hover over the label and there could be something in the tooltip that clarifies it for you. If it still doesn't make sense, feel free to create a github issue about it and I could fix it to be more clear / rework it.

The tray provides shortcuts to the user provided folders and commonly used functions, they can be pretty useful.

## To be implemented  

- Downloading episode ranges (e.g. 12-13)
- Detecting and downloading two part episodes (e.g. Re: Zero 01a & 01b)
- Problems detailing with second/future seasons of anime (e.g. The 'Nanbaka' series episode 14 is another MAL series ID all together, episode 1)

---

### Build & Run

**APIs**  

To build and run this with full functionality, you will have to modify [the api keys container](anime-downloader/Classes/ApiKeys.cs) with your own keys before compiling.  

\- [AniList API](https://anilist.co/settings/developer/)  

**Requirements:** [nuget.exe](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe) on PATH, Visual Studio 2017 and/or C# 7.0 Roslyn Compiler  
**Optional:** Devenv (Visual Studio 2017) on PATH  

```
git clone https://github.com/dukemiller/anime-downloader.git
cd anime-downloader
nuget install anime-downloader\packages.config -OutputDirectory packages
```  

**Building with Devenv (CLI):** ``devenv anime-downloader.sln /Build``  
**Building with Visual Studio:**  Open (Ctrl+Shift+O) "anime-downloader.sln", Build Solution (Ctrl+Shift+B)

An "anime-downloader.exe" artifact will be created in the parent anime-downloader folder.

---

### Notes
+ I started this off as my first application and went through a lot of changes (Delegating every event in the MainWindow.xaml.cs + using x:Name in xaml to alter in code behind, to some view binding and binding the datacontext to {View}.xaml.cs and making the "Settings" classes statically accessable from MainWindow.xaml.cs, to actually using ViewModels and some MVVM practices). What i'm saying is that there's probably some bugs here and there that need fixing and weird behaviors, so throw up a github issue and i'll fix it pretty quickly.
+ Not tested on other platforms other than **Windows**, but the GUI is vanilla WPF based so that's pretty much the end of the line for Mac/Linux support at the moment. In the future, I could extend the project to have all the services and models be their own solution and use something like GTK# for cross platform support given the demand.

### Small disclaimer

Under no circumstances does this software host or help distribute copyrighted works from various studios. This software is not intermediary to retrieving and downloading media and relies on other software to function. In practice and function, this is a cataloger to retrieve data from already existing **external** sources.

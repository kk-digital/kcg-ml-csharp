# Torrent Downloader

This project provides a command-line tool to create and add torrents using `MonoTorrent` in C#.

## Prerequisites

Ensure you have the following installed:
- .NET SDK 7.0+ ([Download here](https://dotnet.microsoft.com/en-us/download))
- Command-line interface (CLI) like PowerShell, Command Prompt, or Terminal

## Installation

Clone the repository and navigate to the project folder:
```sh
git clone <path>
cd kcg-ml-sharp/Torrent
```

Restore dependencies:
```sh
dotnet restore
```

## Usage

### 1. Creating a Torrent

To create a torrent from a dataset, run:
```sh
dotnet run -- create-torrent "sample-dataset" --piece_size 16777216 --tracker_file "trackers.txt"
```
**Arguments:**
- `sample-dataset` → Name of the dataset (inside `data_dataset/` folder)
- `--piece_size` → Size of each piece in bytes (default: 16MB)
- `--tracker_file` → File containing tracker URLs (default: `cmd/torrent/trackers.txt`)

**Output:**
A `.torrent` file will be created in `data_torrent/`.

### 2. Adding a Torrent

To add a torrent to the client and start downloading:
```sh
dotnet run -- add-torrent --downloadDir "downloads" --port 51413 --datasetName "sample-dataset" --torrentFile "data_torrent/sample-dataset.torrent"
```

**Arguments:**
- `--downloadDir` → Directory where files will be downloaded
- `--port` → Port for the torrent client
- `--datasetNames` → Name(s) of datasets to download
- `--torrentFile` → Path to the `.torrent` file






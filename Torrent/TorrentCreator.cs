using System;
using System.IO;
using CommandLine;
using MonoTorrent;
using MonoTorrent.Client;
using System.Linq;
using TorrentApp;

namespace TorrentCreatorApp
{
    


    public static class TorrentCreator
    {
        public static void CreateTorrentWithMonoTorrent(CreateTorrentOptions options)
{
    string datasetFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "data_dataset", options.DatasetName);
    string datasetCardPath = Path.Combine(Directory.GetCurrentDirectory(), "data_datasetcard", options.DatasetName + ".json");
    string datasetTorrentPath = Path.Combine("torrents", options.DatasetName);

    // Check if dataset folder and dataset card exist
    if (!Directory.Exists(datasetFolderPath))
    {
        Console.WriteLine($"Dataset folder not found: {datasetFolderPath}");
        return;
    }

    if (!File.Exists(datasetCardPath))
    {
        Console.WriteLine($"Dataset card not found: {datasetCardPath}");
        return;
    }

    // Prepare the dataset torrent path
    if (Directory.Exists(datasetTorrentPath))
    {
        Directory.Delete(datasetTorrentPath, true);
    }
    Directory.CreateDirectory(datasetTorrentPath);

    // Copy dataset files and dataset card to the torrent folder
    DirectoryCopy(datasetFolderPath, Path.Combine(datasetTorrentPath, options.DatasetName), true);
    File.Copy(datasetCardPath, Path.Combine(datasetTorrentPath, options.DatasetName, $"{options.DatasetName}.json"), true);

    // Read trackers from the file
    if (!File.Exists(options.TrackerFile))
    {
        Console.WriteLine($"Tracker file not found: {options.TrackerFile}");
        return;
    }

    var trackers = File.ReadAllLines(options.TrackerFile).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

    try
    {
        // Create the torrent from the directory source
        var torrentCreator = new MonoTorrent.TorrentCreator();

        var fileSource = new MonoTorrent.TorrentFileSource(datasetTorrentPath);
        var torrent = torrentCreator.Create(fileSource);  // Create the torrent object

        // Define the output path for the torrent file
        string torrentOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "data_torrent", $"{options.DatasetName}.torrent");

        // Ensure the target directory exists
        string torrentDir = Path.GetDirectoryName(torrentOutputPath);
        if (!Directory.Exists(torrentDir))
        {
            Directory.CreateDirectory(torrentDir);
        }

        // Remove the old file if it exists
        if (File.Exists(torrentOutputPath))
        {
            File.Delete(torrentOutputPath);
        }

        // Write the new torrent file
        File.WriteAllBytes(torrentOutputPath, torrent.Encode());

        Console.WriteLine($"Torrent created successfully at {torrentOutputPath}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error creating torrent: {e.Message}");
    }
}


        // Helper method to copy directories recursively
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories();

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDirName}");

            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            foreach (FileInfo file in dir.GetFiles())
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}

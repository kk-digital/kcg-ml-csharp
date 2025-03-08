using System;
using System.IO;
using Minio;
using CommandLine;
using Utils.MinioHelper;

public class TorrentMinioManager
{
    public static MinioClient ConnectToMinio(string ip)
    {
        return MinioHelper.GetMinioClient(MinioSettings.MinioAccessKey, MinioSettings.MinioSecretKey, ip);
    }

    public static void ListTorrents(MinioClient client)
    {
        var torrents = MinioHelper.GetListOfObjectsWithPrefix(MinioSettings.BucketName, "data_torrent");
        Console.WriteLine("Torrents in MinIO:");
        foreach (string torrent in torrents)
        {
            Console.WriteLine(Path.GetFileName(torrent));
        }
    }

    public static void UploadTorrent(MinioClient client, string datasetName)
    {
        string uploadPath = Path.Combine("data_torrent", datasetName + ".torrent");
        if (File.Exists(uploadPath))
        {
            Console.WriteLine($"Uploading {datasetName}.torrent to MinIO...");
            MinioHelper.UploadFromFile( MinioSettings.BucketName, uploadPath, uploadPath);
            Console.WriteLine($"Successfully uploaded {datasetName}.torrent.");
        }
        else
        {
            Console.WriteLine($"Torrent {datasetName}.torrent not found in local storage.");
        }
    }

    public static void DownloadTorrent(MinioClient client, string datasetName)
    {
        string downloadPath = Path.Combine("data_torrent", datasetName + ".torrent");
        if (MinioHelper.IsObjectExists(client, MinioSettings.BucketName, datasetName))
        {
            MinioHelper.DownloadObject(MinioSettings.BucketName, downloadPath, downloadPath);
            Console.WriteLine($"Successfully downloaded {datasetName}.torrent.");
        }
        else
        {
            Console.WriteLine($"File {datasetName}.torrent does not exist in MinIO.");
        }
    }

    public static void RemoveTorrent(MinioClient client, string datasetName)
    {
        string filePath = Path.Combine("data_torrent", datasetName + ".torrent");
        if (MinioHelper.IsObjectExists(client, MinioSettings.BucketName, filePath))
        {
            MinioHelper.RemoveObject( MinioSettings.BucketName, filePath);
            Console.WriteLine($"Successfully removed {datasetName}.torrent.");
        }
        else
        {
            Console.WriteLine($"Torrent {datasetName}.torrent does not exist in MinIO.");
        }
    }

   
}

// Renamed the class to TorrentCommandOptions to avoid conflict with other Options classes
public class TorrentCommandOptions
{
    [Option('d', "dataset", Required = true, HelpText = "Name of the dataset.")]
    public string DatasetName { get; set; }

    [Option('c', "command", Required = true, HelpText = "Command to execute (list, upload, download, remove).")]
    public string Command { get; set; }

    [Option('p', "public", Default = false, HelpText = "Use public MinIO instance.")]
    public bool IsPublic { get; set; }
}

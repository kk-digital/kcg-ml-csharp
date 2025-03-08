using MonoTorrent;
using MonoTorrent.Client;
using CommandLine;
using System.Threading.Tasks;

public static class TorrentAdder
{
    public static async Task AddTorrent(ClientEngine client, string torrentFile, string downloadDir)
    {
        // Load the torrent file
        Torrent torrent = Torrent.Load(torrentFile);

        // Define the settings for the TorrentManager
        TorrentSettings settings = new TorrentSettings();

        // Create the TorrentManager using the AddAsync method
        await client.AddAsync(torrent, downloadDir, settings);

        Console.WriteLine($"Added torrent '{torrent.Name}' for downloading.");
    }

    public static void ListTorrents(ClientEngine client)
    {
        // Iterate over each TorrentManager in the ClientEngine's torrent list
        foreach (var manager in client.Torrents)
        {
            var torrent = manager.Torrent;
            var status = manager.State;
            var metrics = manager.Monitor;

            // Assuming metrics.BytesDownloaded is a field/property
            long bytesDownloaded = metrics.DataBytesReceived;

            string torrentInfo = $"Torrent {torrent.Name}\n" +
                                 $"Status: {status}\n" +
                                 $"Download rate: {Utility.ConvertSizeToReadable(metrics.DownloadRate)}\n" +
                                 $"Upload rate: {Utility.ConvertSizeToReadable(metrics.UploadRate)}\n" +
                                 $"Peers: {manager.Peers}\n" +  // Corrected this to use Count
                                 $"Download remaining: {Utility.ConvertSizeToReadable(torrent.Size - bytesDownloaded)}\n" + // Corrected subtraction
                                 $"Total size: {Utility.ConvertSizeToReadable(torrent.Size)}";
            Console.WriteLine(torrentInfo);
        }
    }
}

public class CommandLineOptions
{
    [Option('d', "downloadDir", Required = true, HelpText = "Directory to download the dataset.")]
    public string DownloadDir { get; set; }

    [Option('p', "port", Required = true, HelpText = "Port to listen on.")]
    public int Port { get; set; }

    [Option('n', "datasetNames", Required = true, HelpText = "Names of the datasets to download.")]
    public string[] DatasetNames { get; set; }
}

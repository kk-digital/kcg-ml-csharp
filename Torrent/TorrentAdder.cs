using MonoTorrent;
using MonoTorrent.Client;
using CommandLine;
using System.Threading.Tasks;

public static class TorrentAdder
{
    public static async Task AddTorrent(ClientEngine client, string torrentFile, string downloadDir)
    {
        // Ensure the download directory exists
        if (!Directory.Exists(downloadDir))
        {
            Directory.CreateDirectory(downloadDir);
            Console.WriteLine($"Created directory: {downloadDir}");
        }

        // Load the torrent file
        Torrent torrent = Torrent.Load(torrentFile);

        // Define the settings for the TorrentManager
        TorrentSettings settings = new TorrentSettings();

        // Create the TorrentManager using the AddAsync method
        var manager = await client.AddAsync(torrent, downloadDir, settings);
        
        // Add additional logging to check the manager state
        Console.WriteLine($"Torrent '{torrent.Name}' added with state: {manager.State}");

        // Check the current state of the torrent and handle it
        switch (manager.State)
        {
            case TorrentState.Stopped:
                Console.WriteLine($"Torrent '{torrent.Name}' is stopped. Starting torrent...");
                await manager.StartAsync();  // Start the torrent if it's stopped
                break;
            case TorrentState.Paused:
                Console.WriteLine($"Torrent '{torrent.Name}' is paused. Resuming torrent...");
                await manager.StartAsync();  // Resume the torrent if it's paused
                break;
            case TorrentState.Downloading:
                Console.WriteLine($"Torrent '{torrent.Name}' is already downloading.");
                break;
            case TorrentState.Seeding:
                Console.WriteLine($"Torrent '{torrent.Name}' has finished downloading and is now seeding.");
                break;
            case TorrentState.Error:
                Console.WriteLine($"Torrent '{torrent.Name}' encountered an error. Please check the logs.");
                break;
            case TorrentState.Metadata:
            case TorrentState.FetchingHashes:
                Console.WriteLine($"Torrent '{torrent.Name}' is fetching metadata or hashes.");
                break;
            default:
                Console.WriteLine($"Torrent '{torrent.Name}' is in state: {manager.State}. No action taken.");
                break;
        }

        // Log the peer connection information
        if (manager.Peers.Available > 0)
        {
            Console.WriteLine($"Active peers: {manager.Peers.Available}");
        }
        else
        {
            Console.WriteLine("No active peers connected.");
        }

        // Optional: Start the engine if it's not already started
        if (!client.IsRunning)
        {
            Console.WriteLine("Starting the ClientEngine...");
            await client.StartAllAsync();  // Ensure the client is running
        }

        // Optional: Give the torrent some time to transition states before listing
        await Task.Delay(3000);  // 1-second delay before listing torrents
        ListTorrents(client);
    }

    public static void ListTorrents(ClientEngine client)
    {
        if (client.Torrents.Count == 0)
        {
            Console.WriteLine("No active torrents found.");
            return;
        }

        // Log each torrent's state for debugging
        foreach (var manager in client.Torrents)
        {
            var torrent = manager.Torrent;
            var status = manager.State;
            var metrics = manager.Monitor;
            long bytesDownloaded = metrics.DataBytesReceived;

            Console.WriteLine($"[DEBUG] Torrent '{torrent.Name}' state: {status}");  // Debugging state

            // Log more information about the peers
            Console.WriteLine($"[DEBUG] Available Peers: {manager.Peers.Available}");
            manager.PeerConnected += (sender, e) =>
            {
                Console.WriteLine($"New peer connected: {e.Peer}");  // Make sure this event fires
            };

            manager.PeerDisconnected += (sender, e) =>
            {
                Console.WriteLine($"Peer disconnected: {e.Peer}");
            };

            // Log the available peers
            if (manager.Peers.Available > 0)
            {
                Console.WriteLine($"Active peers: {manager.Peers.Available}");
            }
            else
            {
                Console.WriteLine("No active peers connected.");
            }

            string torrentInfo = $"Torrent {torrent.Name}\n" +
                                 $"Status: {status}\n" +
                                 $"Download rate: {Utility.ConvertSizeToReadable(metrics.DownloadRate)}\n" +
                                 $"Upload rate: {Utility.ConvertSizeToReadable(metrics.UploadRate)}\n" +
                                 $"Peers: {manager.Peers.Available}\n" +
                                 $"Download remaining: {Utility.ConvertSizeToReadable(torrent.Size - bytesDownloaded)}\n" +
                                 $"Total size: {Utility.ConvertSizeToReadable(torrent.Size)}";
            Console.WriteLine(torrentInfo);
        }
    }
}

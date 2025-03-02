using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

class TorrentSeeder
{
    private static readonly HttpClient client = new HttpClient();
    
    static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: [operation] [dataset_name] [--port=port] [--username=username] [--password=password]");
            return;
        }

        var operation = args[0].ToLower();
        var datasetName = args[1];
        var port = 9092; // default port
        var username = "username";
        var password = "password";

        // Parse optional arguments for port, username, and password
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i].StartsWith("--port="))
            {
                port = int.Parse(args[i].Substring(7));
            }
            else if (args[i].StartsWith("--username="))
            {
                username = args[i].Substring(11);
            }
            else if (args[i].StartsWith("--password="))
            {
                password = args[i].Substring(11);
            }
        }

        string baseUrl = $"http://localhost:{port}/transmission/rpc";
        client.DefaultRequestHeaders.Add("X-Transmission-Session-Id", "");

        try
        {
            // Try connecting to Transmission server and get session ID.
            var sessionResponse = await client.PostAsync(baseUrl, null);
            string sessionId = sessionResponse.Headers.Contains("X-Transmission-Session-Id") 
                                ? sessionResponse.Headers.GetValues("X-Transmission-Session-Id").First() 
                                : string.Empty;

            // Add headers for authentication.
            client.DefaultRequestHeaders.Add("Authorization", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}")));

            // Perform the required operation
            switch (operation)
            {
                case "add":
                    await AddTorrentAsync(baseUrl, datasetName, sessionId);
                    break;

                case "remove":
                    await RemoveTorrentAsync(baseUrl, datasetName, sessionId);
                    break;

                case "stop":
                    await StopTorrentAsync(baseUrl, datasetName, sessionId);
                    break;

                case "continue":
                    await ContinueTorrentAsync(baseUrl, datasetName, sessionId);
                    break;

                case "list":
                    await ListTorrentsAsync(baseUrl, sessionId);
                    break;

                default:
                    Console.WriteLine("Invalid operation.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static async Task AddTorrentAsync(string baseUrl, string datasetName, string sessionId)
    {
        string torrentFilePath = Path.Combine(Directory.GetCurrentDirectory(), "data_torrent", $"{datasetName}.torrent");
        string downloadDir = Path.Combine(Directory.GetCurrentDirectory(), "torrents", datasetName);

        if (!File.Exists(torrentFilePath))
        {
            Console.WriteLine($"Error: Torrent file '{torrentFilePath}' not found.");
            return;
        }

        var requestBody = new
        {
            method = "torrent-add",
            arguments = new
            {
                filename = torrentFilePath,
                download_dir = downloadDir
            }
        };

        await SendRequestAsync(baseUrl, sessionId, requestBody);
        Console.WriteLine($"Added torrent '{datasetName}' to Transmission client.");
    }

    static async Task RemoveTorrentAsync(string baseUrl, string datasetName, string sessionId)
    {
        var torrents = await GetTorrentsAsync(baseUrl, sessionId);
        foreach (var torrent in torrents)
        {
            if (torrent.name == datasetName)
            {
                var requestBody = new
                {
                    method = "torrent-remove",
                    arguments = new { ids = new[] { torrent.id } }
                };

                await SendRequestAsync(baseUrl, sessionId, requestBody);
                Console.WriteLine($"Removed torrent '{datasetName}' (ID: {torrent.id}).");
                return;
            }
        }

        Console.WriteLine($"No torrent found with the name '{datasetName}'.");
    }

    static async Task StopTorrentAsync(string baseUrl, string datasetName, string sessionId)
    {
        var torrents = await GetTorrentsAsync(baseUrl, sessionId);
        foreach (var torrent in torrents)
        {
            if (torrent.name == datasetName)
            {
                var requestBody = new
                {
                    method = "torrent-stop",
                    arguments = new { ids = new[] { torrent.id } }
                };

                await SendRequestAsync(baseUrl, sessionId, requestBody);
                Console.WriteLine($"Stopped torrent '{datasetName}' (ID: {torrent.id}).");
                return;
            }
        }

        Console.WriteLine($"No torrent found with the name '{datasetName}'.");
    }

    static async Task ContinueTorrentAsync(string baseUrl, string datasetName, string sessionId)
    {
        var torrents = await GetTorrentsAsync(baseUrl, sessionId);
        foreach (var torrent in torrents)
        {
            if (torrent.name == datasetName)
            {
                var requestBody = new
                {
                    method = "torrent-start",
                    arguments = new { ids = new[] { torrent.id } }
                };

                await SendRequestAsync(baseUrl, sessionId, requestBody);
                Console.WriteLine($"Resumed torrent '{datasetName}' (ID: {torrent.id}).");
                return;
            }
        }

        Console.WriteLine($"No torrent found with the name '{datasetName}'.");
    }

    static async Task ListTorrentsAsync(string baseUrl, string sessionId)
    {
        var torrents = await GetTorrentsAsync(baseUrl, sessionId);
        foreach (var torrent in torrents)
        {
            Console.WriteLine($"Torrent: {torrent.name} (ID: {torrent.id})");
            Console.WriteLine($"Status: {torrent.status}");
            Console.WriteLine($"Download rate: {torrent.download_rate}");
            Console.WriteLine($"Upload rate: {torrent.upload_rate}");
            Console.WriteLine($"Peers: {torrent.peers_connected}");
            Console.WriteLine($"Download remaining: {torrent.download_remaining}");
            Console.WriteLine($"Total size: {torrent.total_size}");
            Console.WriteLine($"Magnet link: {torrent.magnet_link}\n");
        }
    }

    static async Task SendRequestAsync(string baseUrl, string sessionId, object requestBody)
    {
        var jsonRequest = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Clear();
        if (!string.IsNullOrEmpty(sessionId))
        {
            client.DefaultRequestHeaders.Add("X-Transmission-Session-Id", sessionId);
        }

        var response = await client.PostAsync(baseUrl, content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response: {responseContent}");
    }

    static async Task<dynamic[]> GetTorrentsAsync(string baseUrl, string sessionId)
    {
        var requestBody = new
        {
            method = "torrent-get",
            arguments = new { fields = new[] { "id", "name", "status", "download_rate", "upload_rate", "peers_connected", "download_remaining", "total_size", "magnet_link" } }
        };

        var jsonRequest = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Clear();
        if (!string.IsNullOrEmpty(sessionId))
        {
            client.DefaultRequestHeaders.Add("X-Transmission-Session-Id", sessionId);
        }

        var response = await client.PostAsync(baseUrl, content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
        return jsonResponse?.arguments?.torrents.ToObject<dynamic[]>();
    }
}

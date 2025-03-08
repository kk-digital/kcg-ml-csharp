using System;
using System.Threading.Tasks;
using CommandLine;
using MonoTorrent.Client;

namespace TorrentApp
{
    class Program
    {
       static async Task Main(string[] args)
{
    try
    {
        // Parse command-line arguments
        var parserResult = Parser.Default.ParseArguments<CreateTorrentOptions, AddTorrentOptions>(args);

        // Handle CreateTorrentOptions (Torrent Creation)
        await parserResult.MapResult(
            async (CreateTorrentOptions opts) =>
            {
                // Debugging output for CreateTorrentOptions
                Console.WriteLine($"Creating torrent for dataset: {opts.DatasetName}");
                try
                {
                    await Task.Run(() => TorrentCreatorApp.TorrentCreator.CreateTorrentWithMonoTorrent(opts));
                    Console.WriteLine($"Torrent created successfully for {opts.DatasetName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating torrent for {opts.DatasetName}: {ex.Message}");
                }
            },
            errs =>
            {
                Console.WriteLine("Error parsing CreateTorrentOptions");
                return Task.CompletedTask;
            });

        // Handle AddTorrentOptions (Adding to client)
        await parserResult.MapResult(
            async (AddTorrentOptions opts) =>
            {
                // Debugging output for AddTorrentOptions
                Console.WriteLine($"Adding torrent from file: {opts.TorrentFile}");
                var client = new ClientEngine();  // You need to create or obtain an instance of ClientEngine
                try
                {
                    await TorrentAdder.AddTorrent(client, opts.TorrentFile, opts.DownloadDir);
                    Console.WriteLine($"Torrent added successfully from {opts.TorrentFile}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding torrent: {ex.Message}");
                }
            },
            errs =>
            {
                Console.WriteLine("Error parsing AddTorrentOptions");
                return Task.CompletedTask;
            });
    }
    catch (Exception ex)
    {
        // Catch any unhandled exceptions
        Console.WriteLine($"Unhandled exception: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}
    }

    // Command-line options for creating torrents
    [Verb("create-torrent", HelpText = "Create a torrent file from a dataset.")]
    public class CreateTorrentOptions
    {
        [Value(0, MetaName = "dataset_name", Required = true, HelpText = "Dataset name to be used to create a torrent file.")]
        public string DatasetName { get; set; }

        [Option('p', "piece_size", Default = 16 * 1024 * 1024, HelpText = "Piece size for the torrent file in bytes (default: 16 MiB).")]
        public int PieceSize { get; set; }

        [Option("tracker_file", Default = "cmd/torrent/trackers.txt", HelpText = "Path to the tracker file.")]
        public string TrackerFile { get; set; }

        [Option("private", HelpText = "Flag to create a private torrent (default: false).")]
        public bool Private { get; set; }
    }

    // Command-line options for adding torrents to the client
    [Verb("add-torrent", HelpText = "Add a torrent to the client.")]
    public class AddTorrentOptions
    {
        [Option('d', "downloadDir", Required = true, HelpText = "Directory to download the dataset.")]
        public string DownloadDir { get; set; }

        [Option('p', "port", Required = true, HelpText = "Port to listen on.")]
        public int Port { get; set; }

        [Option('n', "datasetNames", Required = true, HelpText = "Names of the datasets to download.")]
        public string[] DatasetNames { get; set; }

        // Add the TorrentFile property
        [Option('f', "torrentFile", Required = true, HelpText = "Path to the torrent file.")]
        public string TorrentFile { get; set; }  // Add this property
    }
}

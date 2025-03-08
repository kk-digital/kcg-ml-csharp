using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Shared;
using TorchSharp;
using Settings = NanoGptSetting.Settings;
using NanoGptGenerating;
using NanoGptTraining;
using LogUtility;
using FileLib;
// ReSharper disable All
#pragma warning disable IDE0059

namespace NanoGpt;
public class Program
{
    public void Main(string[] args)
    {
        ParseCommandLineArguments(args);

        if (Settings.Device.type == DeviceType.CUDA)
        {
            torch.InitializeDeviceType(DeviceType.CUDA);
        }

        // Set a manual seed for reproducibility
        torch.manual_seed(1337);

        // necessary to use absolute path of input.txt
        string text = FileUtils.ReadAllText(Settings.TrainFile);

        // Create a vocabulary from unique characters
        char[] chars = text.Distinct().OrderBy(c => c).ToArray();
        var vocabSize = chars.Length;

        LibLog.LogInfo($"Vocab size: {vocabSize}");
        LibLog.LogInfo("Vocab: " + string.Join("", chars));

        // Token encoder to convert characters to and from tokens/IDs
        TokenEncoder tokenEncoder = new TokenEncoder(chars);

        if (Settings.Mode == Mode.Train)
        {
            Training.Train(tokenEncoder, text, vocabSize);
        }
        else
        {
            Generating.Generate(tokenEncoder, vocabSize);
        }
    }

    public void ParseCommandLineArguments(string[] args)
    {
        Option<string> runningModeOption = new Option<string>(
            "--running-mode",
            "Let NanoGpt run in <inference | training> mode, training by default"
        );

        Option<string> trainFileOption = new Option<string>(
            "--train-file",
            "Absolute path of input file for training"
        );

        Option<string> nanoGptWeightDataDirOption = new Option<string>(
            "--weight-dir",
            "Absolute path of directory containing NanoGpt weight data"
        );

        RootCommand rootCommand = new RootCommand("NanoGpt app")
        {
            runningModeOption,
            trainFileOption,
            nanoGptWeightDataDirOption
        };

        rootCommand.Handler = CommandHandler.Create<string, string, string>(
            (string runningMode, string trainFile, string weightDir) =>
            {
                if (runningMode.Equals("inference"))
                {
                    Settings.Mode = Mode.Generate;
                    LibLog.LogInfo("Set NanoGPT to run in inference mode");
                }
                else if (runningMode.Equals("training"))
                {
                    Settings.Mode = Mode.Train;
                    LibLog.LogInfo("Set NanoGPT to run in training mode");
                }
                else
                {
                    Settings.Mode = Mode.Train;
                    LibLog.LogInfo("Invalid parameter for --running-mode, defaulting to training mode");
                }

                if (!string.IsNullOrEmpty(trainFile))
                {
                    Settings.TrainFile = trainFile;
                }
                LibLog.LogInfo($"Training file: {Settings.TrainFile}");

                if (!string.IsNullOrEmpty(weightDir))
                {
                    Settings.NanoGptWeightDataDir = weightDir;
                }
                LibLog.LogInfo($"Weight data directory: {Settings.NanoGptWeightDataDir}");
            }
        );

        rootCommand.InvokeAsync(args);
    }
}
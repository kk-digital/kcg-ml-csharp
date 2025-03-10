using Shared;
using System.CommandLine;
using System.CommandLine.Invocation;
using TorchSharp;
using LogUtility;
using FileLib;

namespace NanoGptArgsSettings
{
    public class ArgsSettings
    {
        public string inputTrainData = "../nano-gpt/input.txt";
        public string savedWeightDataDir = "C:/Models/";
        public  Mode Mode = Mode.Train;

        public string SaveLocation(int vocabSize, string settingsKey) => $"{this.savedWeightDataDir}/NanoGpt_{settingsKey}_{vocabSize}.dat";

        public ArgsSettings(string[] args)
        {
            ParseCommandLineArguments(args);
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
                        this.Mode = Mode.Generate;
                        LibLog.LogInfo("Set NanoGPT to run in inference mode");
                    }
                    else if (runningMode.Equals("training"))
                    {
                        this.Mode = Mode.Train;
                        LibLog.LogInfo("Set NanoGPT to run in training mode");
                    }
                    else
                    {
                        this.Mode = Mode.Train;
                        LibLog.LogInfo("Invalid parameter for --running-mode, defaulting to training mode");
                    }

                    if (!string.IsNullOrEmpty(trainFile))
                    {
                        this.inputTrainData = trainFile;
                    }
                    LibLog.LogInfo($"Training file: {this.inputTrainData}");

                    if (!string.IsNullOrEmpty(weightDir))
                    {
                        this.savedWeightDataDir = weightDir;
                    }
                    LibLog.LogInfo($"Weight data directory: {this.savedWeightDataDir}");
                }
            );

            rootCommand.InvokeAsync(args);
        }
    }
}
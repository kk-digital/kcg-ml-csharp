using System;
using System.Threading.Tasks;
using Shared;
using TorchSharp;
using NanoGptArgsSettings;
using Settings = NanoGptSetting.Settings;
using NanoGptGenerating;
using NanoGptTraining;
using LogUtility;
using FileLib;
// ReSharper disable All
#pragma warning disable IDE0059

namespace NanoGpt;
public static class Program
{
    public static void Main(string[] args)
    {
        ArgsSettings argsSettings = new ArgsSettings(args);

        if (Settings.Device.type == DeviceType.CUDA)
        {
            torch.InitializeDeviceType(DeviceType.CUDA);
        }

        // Set a manual seed for reproducibility
        torch.manual_seed(1337);

        // necessary to use absolute path of input.txt
        string text = FileUtils.ReadAllText(argsSettings.inputTrainData);

        // Create a vocabulary from unique characters
        char[] chars = text.Distinct().OrderBy(c => c).ToArray();
        var vocabSize = chars.Length;

        LibLog.LogInfo($"Vocab size: {vocabSize}");
        LibLog.LogInfo("Vocab: " + string.Join("", chars));

        // Token encoder to convert characters to and from tokens/IDs
        TokenEncoder tokenEncoder = new TokenEncoder(chars);

        if (argsSettings.Mode == Mode.Train)
        {
            Training.Train(tokenEncoder, text, vocabSize, argsSettings);
        }
        else
        {
            Generating.Generate(tokenEncoder, vocabSize, argsSettings);
        }
    }
}
using Shared;
using TorchSharp;
using NanoGptArgsSettings;
using Settings = NanoGptSetting.Settings;
using Gpt;
using FileLib;

namespace NanoGptGenerating
{
    public class Generating
    {
        public static void Generate(TokenEncoder tokenEncoder, int vocabSize, ArgsSettings argsSettings)
        {
            GptLanguageModel model = new GptLanguageModel("My_Language_Model", vocabSize).to(Settings.Device);
            if (FileUtils.FileExists(argsSettings.SaveLocation(vocabSize, Settings.SettingsKey)))
            {
                model.load(argsSettings.SaveLocation(vocabSize, Settings.SettingsKey));
            }
            model.GenerateAndPrint(tokenEncoder, int.MaxValue);
        }
    }
}
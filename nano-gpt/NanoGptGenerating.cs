using Shared;
using TorchSharp;
using Settings = NanoGptSetting.Settings;
using Gpt;
using FileLib;

namespace NanoGptGenerating
{
    public class Generating
    {
        public static void Generate(TokenEncoder tokenEncoder, int vocabSize)
        {
            GptLanguageModel model = new GptLanguageModel("My_Language_Model", vocabSize).to(Settings.Device);
            if (FileUtils.FileExists(Settings.SaveLocation(vocabSize)))
            {
                model.load(Settings.SaveLocation(vocabSize));
            }
            model.GenerateAndPrint(tokenEncoder, int.MaxValue);
        }
    }
}
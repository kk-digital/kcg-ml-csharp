using Shared;
using TorchSharp;
using BaseSettings = NanoGptBaseSetting.Settings;

namespace NanoGptSetting
{
    // Exact settings from video in comments, will likely cause your GPU to run out of 
    // memory if you try with CUDA
    public static class Settings
    {
        public static string SaveLocation(int vocabSize) => $"C:\\Models\\NanoGpt_{SettingsKey}_{vocabSize}.dat";
        public static string SettingsKey => $"{Device.type}_{BaseSettings.NEmbed}_{BaseSettings.NHead}_{BaseSettings.NLayer}";

        // You will need a good GPU to train this model, not all of us have A100s
        public static torch.Device Device = torch.cuda.is_available() ? torch.CUDA : torch.CPU; // Change to CUDA if you have good gpu and install CUDA driver in shared csproj by uncommenting
    }
}
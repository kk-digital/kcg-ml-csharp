# How to run NanoGpt
## Run NanoGpt on Linux:
    1.  ensure that you have installed the nvida driver, check with the command "nvidia-smi".

    2.  install cuda referring to [Nvdia CUDA installation guide](https://developer.nvidia.com/cuda-11-6-0-download-archive?target_os=Linux&target_arch=x86_64&Distribution=Ubuntu&target_version=20.04&target_type=deb_local)

    3.  install dotnet:
        1)  source /etc/os-release
        2)  wget https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        3)  sudo dpkg -i packages-microsoft-prod.deb
        4)  rm packages-microsoft-prod.deb
        5)  sudo apt update
        6)  sudo apt-get install -y dotnet-sdk-7.0

    4.  build & run NanoGpt:
        1) run with CPU:
            #1. set "UseCuda" to false in nano-gpt/nano-gpt.csproj
            #2. set "public static Mode Mode" to "Mode.Generate" in nano-gpt/NanoGptSetting.cs for generating, "Mode.Train" for training
            #3. cd nano-gpt-main && dotnet run
            #4. if it raise error asking for absolute path, replace "input.txt" path with absolute path in nano-gpt-main/Program.cs, and set "public static string SaveLocation" with linux absolute path in nano-gpt/NanoGptSetting.cs.

        2) run with GPU:
            #1. set "UseCuda" to true and replace "TorchSharp-cuda-window" with "TorchSharp-cuda-linux"  in nano-gpt/nano-gpt.csproj
            #2. set "public static Mode Mode" to "Mode.Generate" in nano-gpt/NanoGptSetting.cs for generating, "Mode.Train" for training
            #3. cd nano-gpt-main && dotnet run
            #4. if it raise error asking for absolute path, replace "input.txt" path with absolute path in nano-gpt-main/Program.cs, and set "public static string SaveLocation" with linux absolute path in nano-gpt/NanoGptSetting.cs.
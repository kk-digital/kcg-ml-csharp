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

    4.  prepare dotnet package for building:
        dotnet add package System.CommandLine --version 2.0.0-beta1.21308.1

    5.  build & run NanoGpt:
        1) run with CPU:
            #1. set "UseCuda" to false in nano-gpt/nano-gpt.csproj
            #2. cd nano-gpt-main && dotnet build
            #3. ./build/net7.0/nano-gpt-main --running-mode <inference | training> --train-file /path/to/kcg-ml-csharp/nano-gpt/input.txt --weight-dir /path/to/nanogpt/weight/data/dir

        2) run with GPU:
            #1. set "UseCuda" to true and replace "TorchSharp-cuda-window" with "TorchSharp-cuda-linux"  in nano-gpt/nano-gpt.csproj
            #2. cd nano-gpt-main && dotnet build
            #3. ./build/net7.0/nano-gpt-main --running-mode <inference | training> --train-file /path/to/kcg-ml-csharp/nano-gpt/input.txt --weight-dir /path/to/nanogpt/weight/data/dir
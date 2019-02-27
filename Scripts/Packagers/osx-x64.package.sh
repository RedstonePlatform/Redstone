#!/bin/bash
NONE='\033[00m'
RED='\033[01;31m'
GREEN='\033[01;32m'
YELLOW='\033[01;33m'
PURPLE='\033[01;35m'
CYAN='\033[01;36m'
WHITE='\033[01;37m'
BOLD='\033[1m'
UNDERLINE='\033[4m'

runtime="osx-x64"
warp_runtime="macos-x64"
configuration="release"
git_commit=$(git log --format=%h --abbrev=7 -n 1)
publish_directory="/tmp/Redstone/Release/Publish"
release_directory="/tmp/Redstone/Release"
download_directory="/tmp"
warp="${warp_runtime}.warp-packer"
project_path="../../src/Redstone/Programs/Redstone.RedstoneFullNodeD/Redstone.RedstoneFullNodeD.csproj"
OS_VER="Ubuntu*"

function check_root {
if [ "$(id -u)" != "0" ]; then
    echo "Sorry, this script needs to be run as root. Do \"sudo su root\" and then re-run this script"
    exit 1
fi
}

function DisplayParameters {
echo warp is ${warp}
echo "Download directory is:" $download_directory
echo "Publish directory is: " $publish_directory
echo "Release directory is: " $release_directory
echo "Download file is " ${download_directory}/${warp}
echo "Current directory is:" $PWD 
echo "Git commit to build:" $git_commit
}

function DownloadWarp {
echo "Downloading warp..."
curl -L -o ${download_directory}/${warp} "https://github.com/stratisproject/warp/releases/download/v0.2.1/${warp}"
if [ -f "${download_directory}/${warp}" ]; then
   echo "Warp packer downloaded succesfully."
else
   echo "Warp packer didn't download successfully."
fi
}

function compileWallet {
echo "Building the full node..."
mkdir -p $publish_directory
dotnet --info
dotnet publish $project_path -c $configuration -v m -r $runtime  --self-contained --no-dependencies -o $publish_directory

echo "List of files to package:" 
ls $publish_directory

echo "Packaging the daemon..."
chmod +x "${download_directory}/${warp}"

"${download_directory}/${warp}" --arch ${warp_runtime} --input_dir ${publish_directory} --exec Redstone.RedstoneFullNodeD --output ${release_directory}/Redstone-${runtime}-${git_commit}
chmod +x "${release_directory}/Redstone-${runtime}-${git_commit}"
rm -rf ${publish_directory}
echo "Done."
}

clear
echo && echo
echo -e ${RED}
echo -e "${RED}██████╗ ███████╗██████╗ ███████╗████████╗ ██████╗ ███╗   ██╗███████╗${NONE}"  
echo -e "${RED}██╔══██╗██╔════╝██╔══██╗██╔════╝╚══██╔══╝██╔═══██╗████╗  ██║██╔════╝${NONE}"    
echo -e "${RED}██████╔╝█████╗  ██║  ██║███████╗   ██║   ██║   ██║██╔██╗ ██║█████╗  ${NONE}"    
echo -e "${RED}██╔══██╗██╔══╝  ██║  ██║╚════██║   ██║   ██║   ██║██║╚██╗██║██╔══╝  ${NONE}"    
echo -e "${RED}██║  ██║███████╗██████╔╝███████║   ██║   ╚██████╔╝██║ ╚████║███████╗${NONE}"    
echo -e "${RED}╚═╝  ╚═╝╚══════╝╚═════╝ ╚══════╝   ╚═╝    ╚═════╝ ╚═╝  ╚═══╝╚══════╝${NONE}"  
echo -e ${RED}
echo -e "${PURPLE}**********************************************************************${NONE}"
echo -e "${PURPLE}*    ${NONE}This script will compile the full node for ${runtime}.               *${NONE}"
echo -e "${PURPLE}**********************************************************************${NONE}"
echo -e "${BOLD}"
read -p "Please run this script as the root user. Do you want to compile full node for ${runtime} (y/n)?" response
echo

echo -e "${NONE}"

if [[ "$response" =~ ^([yY][eE][sS]|[yY])+$ ]]; then

    check_root
    DownloadWarp
    compileWallet
    DisplayParameters
	
echo
echo -e "${GREEN} Installation complete. ${NONE}"
echo -e "${GREEN} thecrypt0hunter(2018)${NONE}"
else

   echo && echo -e "${RED} Installation cancelled! ${NONE}" && echo
fi


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

runtime="linux-arm"
configuration="release"
git_commit=$(git log --format=%h --abbrev=7 -n 1)
publish_directory="/tmp/Redstone/Release/Publish"
release_directory="/tmp/Redstone/Release"
project_path="../../src/Redstone/Programs/Redstone.RedstoneFullNodeD/Redstone.RedstoneFullNodeD.csproj"
OS_VER="Ubuntu*"

function check_root {
if [ "$(id -u)" != "0" ]; then
    echo "Sorry, this script needs to be run as root. Do \"sudo su root\" and then re-run this script"
    exit 1
fi
}

function DisplayParameters {
echo "Publish directory is:" $publish_directory
echo "Project file is     :" ${project_path}
echo "Current directory is:" $PWD 
echo "Git commit to build :" $git_commit
}

function compileWallet {
    echo -e "* Compiling wallet. Please wait, this might take a while to complete..."
    #dotnet --info  
    mkdir -p $publish_directory
    dotnet publish $project_path -c $configuration -v m -r $runtime --no-dependencies -o $publish_directory #--self-contained
    cd $publish_directory
    tar -cvf $release_directory/Redstone-$runtime-$git_commit.tar *
    rm -rf $publish_directory
    echo -e "${NONE}${GREEN}* Done${NONE}"
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
    compileWallet
    DisplayParameters
	
echo
echo -e "${GREEN} Installation complete. ${NONE}"
echo -e "${GREEN} thecrypt0hunter(2018)${NONE}"
else

   echo && echo -e "${RED} Installation cancelled! ${NONE}" && echo
fi


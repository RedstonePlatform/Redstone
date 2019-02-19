#/bin/bash
NONE='\033[00m'
RED='\033[01;31m'
GREEN='\033[01;32m'
YELLOW='\033[01;33m'
PURPLE='\033[01;35m'
CYAN='\033[01;36m'
WHITE='\033[01;37m'
BOLD='\033[1m'
UNDERLINE='\033[4m'
MAX=12

declare -r MNODE_USER=redstone
declare -r CONF=release
declare -r COINGITHUB=https://github.com/RedstonePlatform/Redstone.git
declare -r COINPORT=19156
declare -r COINRPCPORT=19157
declare -r COINDAEMON=redstoned
declare -r COINCLI=redstone-cli
declare -r COINTX=redstone-tx
declare -r COINCORE=/home/${MNODE_USER}/.stratisnode/redstone/RedstoneTest
declare -r COINCONFIG=redstone.conf
declare -r COINRUNCMD="sudo dotnet ./Redstone.RedstoneFullNodeD.dll -testnet -datadir=/home/${NODE_USER}/.redstonenode" #'sudo dotnet run -testnet'
declare -r COINSTARTUP=/home/${MNODE_USER}/redstoned
declare -r COINSRCLOC=/home/${MNODE_USER}/Redstone
declare -r COINDLOC=/home/${MNODE_USER}/RedstoneNode   
declare -r COINDSRC=/home/${MNODE_USER}/Redstone/src/Redstone/Programs/Redstone.RedstoneFullNodeD
declare -r COINDEXE=${COINDSRC}/bin/${CONF}/netcoreapp2.1
declare -r COINSERVICELOC=/etc/systemd/system/
declare -r COINSERVICENAME=${COINDAEMON}@${MNODE_USER}
declare -r DATE_STAMP="$(date +%y-%m-%d-%s)"
declare -r SCRIPT_LOGFILE="/tmp/redstone_${DATE_STAMP}_out.log"
declare -r SWAPSIZE="1G"
declare -r ONION_ADDR=""

function check_root() {
if [ "$(id -u)" != "0" ]; then
    echo "Sorry, this script needs to be run as root. Do \"sudo su root\" and then re-run this script"
    exit 1
fi
}

create_mn_user() {
    echo
    echo "* Checking for user & add if required. Please wait..."
    # our new mnode unpriv user acc is added
    if id "${MNODE_USER}" >/dev/null 2>&1; then
        echo "user exists already, do nothing"
    else
        echo -e "${NONE}${GREEN}* Adding new system user ${MNODE_USER}${NONE}"
        sudo adduser --disabled-password --gecos "" ${MNODE_USER} &>> ${SCRIPT_LOGFILE}
        sudo echo -e "${MNODE_USER} ALL=(ALL) NOPASSWD:ALL" &>> /etc/sudoers.d/90-cloud-init-users

    fi
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

function set_permissions() {
	# maybe add a sudoers entry later
	chown -R ${MNODE_USER}:${MNODE_USER} ${COINCORE} ${COINSTARTUP} ${COINSRCLOC} ${COINSTARTUP} ${COINDLOC} &>> ${SCRIPT_LOGFILE}
    # make group permissions same as user, so vps-user can be added to masternode group
    chmod -R g=u ${COINCORE} ${COINSTARTUP} ${COINSRCLOC} ${COINSTARTUP} ${COINDLOC} ${COINSERVICELOC} &>> ${SCRIPT_LOGFILE}
}

checkForUbuntuVersion() {
   echo
   echo "* Checking Ubuntu version..."
    if [[ `cat /etc/issue.net`  == *16.04* ]]; then
        echo -e "${GREEN}* You are running `cat /etc/issue.net` . Setup will continue.${NONE}";
    else
        echo -e "${RED}* You are not running Ubuntu 16.04.X. You are running `cat /etc/issue.net` ${NONE}";
        echo && echo "Installation cancelled" && echo;
        exit;
    fi
}

updateAndUpgrade() {
    echo
    echo "* Running update and upgrade. Please wait..."
    sudo DEBIAN_FRONTEND=noninteractive apt-get update -qq -y &>> ${SCRIPT_LOGFILE}
    sudo DEBIAN_FRONTEND=noninteractive apt-get upgrade -y -qq &>> ${SCRIPT_LOGFILE}
    sudo DEBIAN_FRONTEND=noninteractive apt-get autoremove -y -qq &>> ${SCRIPT_LOGFILE}
    echo -e "${GREEN}* Done${NONE}";
}

setupSwap() {
#check if swap is available
    echo
    echo "* Creating Swap File. Please wait..."
if [ $(free | awk '/^Swap:/ {exit !$2}') ] || [ ! -f "/var/mnode_swap.img" ];then
    echo "* No proper swap, creating it"
    # needed because ant servers are ants
    sudo rm -f /var/mnode_swap.img &>> ${SCRIPT_LOGFILE}
    sudo dd if=/dev/zero of=/var/mnode_swap.img bs=1024k count=${SWAPSIZE} &>> ${SCRIPT_LOGFILE}
    sudo chmod 0600 /var/mnode_swap.img &>> ${SCRIPT_LOGFILE}
    sudo mkswap /var/mnode_swap.img &>> ${SCRIPT_LOGFILE}
    sudo swapon /var/mnode_swap.img &>> ${SCRIPT_LOGFILE}
    echo '/var/mnode_swap.img none swap sw 0 0' | tee -a /etc/fstab &>> ${SCRIPT_LOGFILE}
    echo 'vm.swappiness=10' | tee -a /etc/sysctl.conf               &>> ${SCRIPT_LOGFILE}
    echo 'vm.vfs_cache_pressure=50' | tee -a /etc/sysctl.conf		&>> ${SCRIPT_LOGFILE}
else
    echo "* All good, we have a swap"
fi
}

installFail2Ban() {
    echo
    echo -e "* Installing fail2ban. Please wait..."
    sudo apt-get -y install fail2ban &>> ${SCRIPT_LOGFILE}
    sudo systemctl enable fail2ban &>> ${SCRIPT_LOGFILE}
    sudo systemctl start fail2ban &>> ${SCRIPT_LOGFILE}
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

setupTmpRAM() {
    echo
    echo -e "* Pushing tmp files to RAM for performance. Please wait..."
    echo 'tmpfs   /tmp            tmpfs   defaults,noatime,nosuid,nodev,noexec,mode=1777,size=512M          0       0' | tee -a /etc/fstab &>> ${SCRIPT_LOGFILE}
    echo 'tmpfs   /var/tmp        tmpfs   defaults,noatime,mode=1777,size=2M                      0       0' | tee -a /etc/fstab &>> ${SCRIPT_LOGFILE}
    echo -e "${NONE}${GREEN}* Done${NONE}";
}


installFirewall() {
    echo
    echo -e "* Installing UFW. Please wait..."
    sudo apt-get -y install ufw &>> ${SCRIPT_LOGFILE}
    sudo ufw allow OpenSSH &>> ${SCRIPT_LOGFILE}
    # required for cockpit
    #sudo ufw allow 9090/tcp     &>> ${SCRIPT_LOGFILE}
    sudo ufw allow $COINPORT/tcp &>> ${SCRIPT_LOGFILE}
    sudo ufw allow $COINRPCPORT/tcp &>> ${SCRIPT_LOGFILE}
    echo "y" | sudo ufw enable &>> ${SCRIPT_LOGFILE}
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

installDependencies() {
    echo
    echo -e "* Installing dependencies. Please wait..."
    sudo timedatectl set-ntp no &>> ${SCRIPT_LOGFILE}
    sudo apt-get install git ntp nano wget curl software-properties-common -y &>> ${SCRIPT_LOGFILE}
    sudo curl -s https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg &>> ${SCRIPT_LOGFILE}
    sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg &>> ${SCRIPT_LOGFILE}
    sudo bash -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-xenial-prod xenial main" > /etc/apt/sources.list.d/dotnetdev.list' &>> ${SCRIPT_LOGFILE}
    sudo apt-get install apt-transport-https -y &>> ${SCRIPT_LOGFILE}
    sudo apt-get update -y &>> ${SCRIPT_LOGFILE}
    sudo apt-get install dotnet-sdk-2.2 -y --allow-unauthenticated &>> ${SCRIPT_LOGFILE}
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

compileWallet() {
    echo
    echo -e "* Compiling wallet. Please wait, this might take a while to complete..."
    cd /home/${MNODE_USER}/
    git clone ${COINGITHUB} &>> ${SCRIPT_LOGFILE}
    cd ${COINSRCLOC} 
    git submodule update --init --recursive &>> ${SCRIPT_LOGFILE}
    cd ${COINDSRC} 
    dotnet build -c ${CONF} -v m &>> ${SCRIPT_LOGFILE}	   ### compile code
    cd ${COINDEXE}
    rm -rf ${COINDLOC} &>> ${SCRIPT_LOGFILE}
    mkdir ${COINDLOC} &>> ${SCRIPT_LOGFILE}
    cp * ${COINDLOC}  &>> ${SCRIPT_LOGFILE}    	### copy executables
    rm -rf ${COINSRCLOC}  &>> ${SCRIPT_LOGFILE} 	### Remove source
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

installWallet() {
    echo
    echo -e "* Installing wallet. Please wait..."
    cd /home/${MNODE_USER}/
    echo -e "#!/bin/bash\nexport DOTNET_CLI_TELEMETRY_OPTOUT=1\ncd $COINDLOC\n$COINRUNCMD" > ${COINSTARTUP}
    echo -e "[Unit]\nDescription=${COINDAEMON}\nAfter=network-online.target\n\n[Service]\nType=simple\nUser=${MNODE_USER}\nGroup=${MNODE_USER}\nExecStart=${COINSTARTUP}\nRestart=always\nRestartSec=5\nPrivateTmp=true\nTimeoutStopSec=60s\nTimeoutStartSec=5s\nStartLimitInterval=120s\nStartLimitBurst=15\n\n[Install]\nWantedBy=multi-user.target" >$COINSERVICENAME.service
    chown -R ${MNODE_USER}:${MNODE_USER} ${COINSERVICELOC} &>> ${SCRIPT_LOGFILE}
    sudo mv $COINSERVICENAME.service $COINSERVICELOC
    sudo chmod 777 ${COINSTARTUP}
    sudo systemctl --system daemon-reload &>> ${SCRIPT_LOGFILE}
    sudo systemctl enable ${COINSERVICENAME} &>> ${SCRIPT_LOGFILE}
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

configureWallet() {
    echo
    echo -e "* Configuring wallet. Please wait..."
    cd /home/${MNODE_USER}/
    mnip=$(curl --silent ipinfo.io/ip)
    rpcuser=`cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1`
    rpcpass=`cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1`
    sudo mkdir -p $COINCORE
    echo -e "externalip=${ONION_ADDR}\ntxindex=1\nlisten=1\ndaemon=1\nmaxconnections=64\n#addnode=\n#addnode=" > $COINCONFIG
    ### Update .conf with TOR config
    sudo sh -c `echo "### TOR CONFIG ###" >> $COINCONFIG`
    sudo sh -c `echo "onion=127.0.0.1:9050" >> $COINCONFIG`
    sudo sh -c `echo "onlynet=tor" >> $COINCONFIG`
    sudo sh -c `echo "bind=127.0.0.1" >> $COINCONFIG`
    sudo sh -c `echo "listen=1" >> $COINCONFIG`
    sudo sh -c `echo "dnsseed=0" >> $COINCONFIG`
    sudo sh -c `echo "### TOR CONF END ###" >> $COINCONFIG`
    sudo mv $COINCONFIG $COINCORE
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

startWallet() {
    echo
    echo -e "* Starting wallet daemon..."
    sudo service ${COINSERVICENAME} start &>> ${SCRIPT_LOGFILE}
    sleep 2
    echo -e "${GREEN}* Done${NONE}";
}
stopWallet() {
    echo
    echo -e "* Stopping wallet daemon..."
    sudo service ${COINSERVICENAME} stop &>> ${SCRIPT_LOGFILE}
    sleep 2
    echo -e "${GREEN}* Done${NONE}";
}

installCockpit() {
    echo
    echo "* Installing Cockpit. Please wait..."
    sudo apt-get install cockpit -y &>> ${SCRIPT_LOGFILE}
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

function installUnattendedUpgrades() {

    echo
    echo "* Installing Unattended Upgrades. Can take a while ... Please wait..."
    sudo apt install unattended-upgrades -y &>> ${SCRIPT_LOGFILE}
    sleep 3
    sudo sh -c `echo "Unattended-Upgrade::Allowed-Origins {" >> /etc/apt/apt.conf.d/50unattended-upgrades`
    sudo sh -c `echo "        "${distro_id}:${distro_codename}";" >> /etc/apt/apt.conf.d/50unattended-upgrades`
    sudo sh -c `echo "        "${distro_id}:${distro_codename}-security";" >> /etc/apt/apt.conf.d/50unattended-upgrades`
    sudo sh -c `echo "APT::Periodic::AutocleanInterval "7";" >> /etc/apt/apt.conf.d/20auto-upgrades`
    sudo sh -c `echo "APT::Periodic::Unattended-Upgrade "1";" >> /etc/apt/apt.conf.d/20auto-upgrades`
    cat /etc/apt/apt.conf.d/20auto-upgrades &>> ${SCRIPT_LOGFILE}
}
function installTOR() {
    echo
    echo "* Installing TOR. Please wait..."
    sudo apt-get install tor -y &>> ${SCRIPT_LOGFILE}

    ## Setup torrc file
    sudo bash -c 'echo "### XERONET ROCKET TORRC for BWK ###" >> /etc/tor/torrc'
    sudo bash -c 'echo "HiddenServiceDir /var/lib/tor/hidden_service/" >> /etc/tor/torrc'
    sudo bash -c 'echo "ClientOnly 1" >> /etc/tor/torrc'
    sudo bash -c 'echo "ControlPort 9051" >> /etc/tor/torrc'
    sudo bash -c 'echo "NumEntryGuards 4" >> /etc/tor/torrc'
    sudo bash -c 'echo "NumDirectoryGuards 3" >> /etc/tor/torrc'
    sudo bash -c 'echo "GuardLifetime 2764800" >> /etc/tor/torrc'
    sudo bash -c 'echo "GeoIPExcludeUnknown 1" >> /etc/tor/torrc'
    sudo bash -c 'echo "EntryNodes 31.185.104.19/32,31.185.104.20/31,46.182.106.190/32,51.15.13.245/32,51.15.43.232/32,51.15.44.197/32,51.15.45.97/32,51.15.46.49/32,51.15.50.133/32,51.15.57.177/32,51.15.57.79/32,51.15.60.255/32,51.15.60.62/32,62.102.148.67/32,62.138.7.171/32,77.109.139.87/32,78.142.140.242/32,80.67.172.162/32,81.7.10.29/32,82.94.251.227/32,85.248.227.163/32,85.248.227.164/31,86.59.119.83/32,86.59.119.88/32,89.234.157.254/32,91.121.23.100/32,94.140.120.44/32,94.242.246.23/32,94.242.246.24/32,94.252.114.48/32,95.142.161.63/32,134.119.3.164/32,171.25.193.20/32,171.25.193.25/32,171.25.193.77/32,171.25.193.78/32,176.10.104.240/32,176.10.104.243/32,176.126.252.11/32,176.126.252.12/32,178.16.208.55/32,178.16.208.56/30,178.16.208.60/31,178.16.208.62/32,178.20.55.16/32,178.20.55.18/32,178.209.42.84/32,185.100.84.82/32,185.100.86.100/32,185.34.33.2/32,185.86.149.75/32,188.118.198.244/32,192.36.27.4/32,192.36.27.6/31,192.42.116.16/32,212.51.156.78/32" >> /etc/tor/torrc'
    sudo bash -c 'echo "ExitNodes 31.185.104.19/32,31.185.104.20/31,46.182.106.190/32,51.15.43.232/32,51.15.44.197/32,51.15.45.97/32,51.15.46.49/32,51.15.50.133/32,51.15.57.177/32,51.15.57.79/32,51.15.60.255/32,51.15.60.62/32,62.102.148.67/32,77.109.139.87/32,80.67.172.162/32,85.248.227.163/32,85.248.227.164/31,89.234.157.254/32,94.242.246.23/32,94.242.246.24/32,95.142.161.63/32,171.25.193.20/32,171.25.193.25/32,171.25.193.77/32,171.25.193.78/32,176.10.104.240/32,176.10.104.243/32,176.126.252.11/32,176.126.252.12/32,178.20.55.16/32,178.20.55.18/32,178.209.42.84/32,185.100.84.82/32,185.100.86.100/32,185.34.33.2/32,192.36.27.4/32,192.36.27.6/31,192.42.116.16/32,212.16.104.33/32" >> /etc/tor/torrc'
    sudo bash -c 'echo "ExcludeNodes default,Unnamed,{ae},{af},{ag},{ao},{az},{ba},{bb},{bd},{bh},{bi},{bn},{bt},{bw},{by},{cd},{cf},{cg},{ci},{ck},{cm},{cn},{cu},{cy},{dj},{dm},{dz},{eg},{er},{et},{fj},{ga},{gd},{gh},{gm},{gn},{gq},{gy},{hr},{ht},{id},{in},{iq},{ir},{jm},{jo},{ke},{kg},{kh},{ki},{km},{kn},{kp},{kw},{kz},{la},{lb},{lc},{lk},{lr},{ly},{ma},{me},{mk},{ml},{mm},{mr},{mu},{mv},{mw},{my},{na},{ng},{om},{pg},{ph},{pk},{ps},{qa},{rs},{ru},{rw},{sa},{sb},{sd},{sg},{si},{sl},{sn},{so},{st},{sy},{sz},{td},{tg},{th},{tj},{tm},{tn},{to},{tr},{tt},{tv},{tz},{ug},{uz},{vc},{ve},{vn},{ws},{ye},{zm},{zw},{??}" >> /etc/tor/torrc'
    sudo bash -c 'echo "ExcludeExitNodes default,Unnamed,{ae},{af},{ag},{ao},{az},{ba},{bb},{bd},{bh},{bi},{bn},{bt},{bw},{by},{cd},{cf},{cg},{ci},{ck},{cm},{cn},{cu},{cy},{dj},{dm},{dz},{eg},{er},{et},{fj},{ga},{gd},{gh},{gm},{gn},{gq},{gy},{hr},{ht},{id},{in},{iq},{ir},{jm},{jo},{ke},{kg},{kh},{ki},{km},{kn},{kp},{kw},{kz},{la},{lb},{lc},{lk},{lr},{ly},{ma},{me},{mk},{ml},{mm},{mr},{mu},{mv},{mw},{my},{na},{ng},{om},{pg},{ph},{pk},{ps},{qa},{rs},{ru},{rw},{sa},{sb},{sd},{sg},{si},{sl},{sn},{so},{st},{sy},{sz},{td},{tg},{th},{tj},{tm},{tn},{to},{tr},{tt},{tv},{tz},{ug},{uz},{vc},{ve},{vn},{ws},{ye},{zm},{zw},{??}" >> /etc/tor/torrc'
    sudo bash -c 'echo "HiddenServiceDir /var/lib/tor/hidden_service/" >> /etc/tor/torrc'
    sudo bash -c 'echo "HiddenServicePort 52543 127.0.0.1:52543" >> /etc/tor/torrc'
    sudo bash -c 'echo "HiddenServicePort 80 127.0.0.1:80" >> /etc/tor/torrc'
    sudo bash -c 'echo "LongLivedPorts 80,52543" >> /etc/tor/torrc'

    ## Add Tor check to crontab
    sudo /etc/init.d/tor stop &>> ${SCRIPT_LOGFILE}
    sleep 1
    sudo touch /etc/cron.d/torcheck &>> ${SCRIPT_LOGFILE}
    sudo bash -c 'echo "*/5 * * * * root /etc/init.d/tor start > /dev/null 2>&1" >> /etc/cron.d/torcheck' ### CHECK ME or USE CRONTAB -e
    sudo rm -R /var/lib/tor/hidden_service &>> ${SCRIPT_LOGFILE}
    sudo /etc/init.d/tor start &>> ${SCRIPT_LOGFILE}
    echo "Tor installed, configured and restarted"

    # Get the .onion address and log .
    ONION_ADDR=$( sudo cat /var/lib/tor/hidden_service/hostname )
    echo "Onion Address: ${ONION_ADDR}" &>> ${SCRIPT_LOGFILE}
    echo "Show Onion Address: ${ONION_ADDR}"
    #Check Tor status and log
    curl --socks5 localhost:9050 --socks5-hostname localhost:9050 -s https://check.torproject.org/ | cat | grep -m 1 Congratulations | xargs  &>> ${SCRIPT_LOGFILE}
}

clear
cd
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
#echo -e "${PURPLE}*                                                                    *${NONE}"
echo -e "${PURPLE}*    ${NONE}This script will install and configure your Redstone node.      *${NONE}"
#echo -e "${PURPLE}*                                                                    *${NONE}"
echo -e "${PURPLE}**********************************************************************${NONE}"
echo -e "${BOLD}"
read -p "Please run this script as the root user. Do you want to setup (y) or upgrade (u) your Redstone node. (y/n/u)?" response
echo

echo -e "${NONE}"

if [[ "$response" =~ ^([yY][eE][sS]|[yY])+$ ]]; then

    check_root
    create_mn_user
    checkForUbuntuVersion
    updateAndUpgrade
    #setupSwap
    setupTmpRAM
    installFail2Ban
    installTOR
    installFirewall
    installDependencies
    compileWallet
    installWallet
    configureWallet
    #installCockpit
    installUnattendedUpgrades
    startWallet
    set_permissions

echo
echo -e "${BLUE} Installation complete. ${NONE}"
echo -e "${BLUE} The log file will be here: ${SCRIPT_LOGFILE}.${NONE}"
echo -e "${BLUE} Check service with: sudo journalctl -f -u ${COINSERVICENAME} ${NONE}"
else
    if [[ "$response" =~ ^([uU])+$ ]]; then
        check_root
        stopWallet
	updateAndUpgrade
        compileWallet
        startWallet
        echo -e "${BLUE} Upgrade complete. Check service with: sudo journalctl -f -u ${COINSERVICENAME} ${NONE}"

    else
      echo && echo -e "${RED} Installation cancelled! ${NONE}" && echo
    fi
    
fi
    cd ~

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

OS_VER="Ubuntu*"
ARCH="linux-x64"
DATE_STAMP="$(date +%y-%m-%d-%s)"
SCRIPT_LOGFILE="/tmp/${NODE_USER}_${DATE_STAMP}_output.log"
NODE_IP=$(curl --silent ipinfo.io/ip)

function setMainVars() {
	## set network dependent variables
	NETWORK=""
	NODE_USER=redstone${NETWORK}
	COINCORE=/home/${NODE_USER}/.redstonenode/redstone/RedstoneMain
	COINPORT=19056
	COINRPCPORT=19057
	COINAPIPORT=37222
	DNSPORT=53
	COIN="XRD"
	RPCBIND='127.0.0.1'
}

function setTestVars() {
	## set network dependent variables
	NETWORK="-testnet"
	NODE_USER=redstone${NETWORK}
	COINCORE=/home/${NODE_USER}/.redstonenode/redstone/RedstoneTest
	COINPORT=19156
	COINRPCPORT=19157
	COINAPIPORT=38222
	DNSPORT=53
	COIN="TXRD"
	RPCBIND='127.0.0.1'
}

function setDNSVars() {
	## set DNS specific variables
	if [ "${NETWORK}" = "" ] ; then
	   DNS="-iprangefiltering=0 -externalip=${NODE_IP} -dnshostname=seed.redstonecoin.com -dnsnameserver=dns1.seed.redstonecoin.com -dnsmailbox=admin@redstoneplatform.com -dnsfullnode=1 -dnslistenport=${DNSPORT}"
	 else
	   DNS="-iprangefiltering=0 -externalip=${NODE_IP} -dnshostname=seed.redstoneplatform.com -dnsnameserver=testdns1.seed.redstoneplatform.com -dnsmailbox=admin@redstoneplatform.com -dnsfullnode=1 -dnslistenport=${DNSPORT}"
	fi
	## Stop port 53 from being used by systemd-resovled
	echo 'DNSStubListener=no' | sudo tee -a /etc/systemd/resolved.conf &>> ${SCRIPT_LOGFILE}
	sudo service systemd-resolved restart
}

function setGeneralVars() {
	## set general variables
	COINRUNCMD="sudo dotnet ./Redstone.RedstoneFullNodeD.dll ${NETWORK} -datadir=/home/${NODE_USER}/.redstonenode -iprangefiltering=0 ${DNS}"  ## additional commands can be used here e.g. -testnet or -stake=1
	CONF=release
	COINGITHUB=https://github.com/RedstonePlatform/Redstone.git
	COINDAEMON=redstoned
	COINCONFIG=redstone.conf
	COINSTARTUP=/home/${NODE_USER}/redstoned
	COINDLOC=/home/${NODE_USER}/RedstoneNode
	COINDSRC=/home/${NODE_USER}/code/src/Redstone/Programs/Redstone.RedstoneFullNodeD
	COINSERVICELOC=/etc/systemd/system/
	COINSERVICENAME=${COINDAEMON}@${NODE_USER}
	SWAPSIZE="1024" ## =1GB
	IPSTACK_APIKEY="3c31f4758711b7e40e5c115c390e8546"
}

function check_root() {
	if [ "$(id -u)" != "0" ]; then
	    echo -e "${RED}* Sorry, this script needs to be run as root. Do \"sudo su root\" and then re-run this script${NONE}"
	    exit 1
	    echo -e "${NONE}${GREEN}* All Good!${NONE}";
	fi
}

function create_user() {
    echo
    echo "* Checking for user & add if required. Please wait..."
    # our new mnode unpriv user acc is added
    if id "${NODE_USER}" >/dev/null 2>&1; then
        echo "user exists already, do nothing"
    else
        echo -e "${NONE}${GREEN}* Adding new system user ${NODE_USER}${NONE}"
        sudo adduser --disabled-password --gecos "" ${NODE_USER} &>> ${SCRIPT_LOGFILE}
        sudo echo -e "${NODE_USER} ALL=(ALL) NOPASSWD:ALL" &>> /etc/sudoers.d/90-cloud-init-users
    fi
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

function set_permissions() {
    chown -R ${NODE_USER}:${NODE_USER} ${COINCORE} ${COINSTARTUP} ${COINDLOC} &>> ${SCRIPT_LOGFILE}
    # make group permissions same as user, so vps-user can be added to node group
    chmod -R g=u ${COINCORE} ${COINSTARTUP} ${COINDLOC} ${COINSERVICELOC} &>> ${SCRIPT_LOGFILE}
}

function checkOSVersion() {
   echo
   echo "* Checking OS version..."
    if [[ `cat /etc/issue.net`  == ${OS_VER} ]]; then
        echo -e "${GREEN}* You are running `cat /etc/issue.net` . Setup will continue.${NONE}";
    else
        echo -e "${RED}* You are not running ${OS_VER}. You are running `cat /etc/issue.net` ${NONE}";
        echo && echo "Installation cancelled" && echo;
        exit;
    fi
}

function updateAndUpgrade() {
    echo
    echo "* Running update and upgrade. Please wait..."
    sudo DEBIAN_FRONTEND=noninteractive apt-get update -qq -y &>> ${SCRIPT_LOGFILE}
    sudo DEBIAN_FRONTEND=noninteractive apt-get upgrade -y -qq &>> ${SCRIPT_LOGFILE}
    sudo DEBIAN_FRONTEND=noninteractive apt-get autoremove -y -qq &>> ${SCRIPT_LOGFILE}
    echo -e "${GREEN}* Done${NONE}";
}

function setupSwap() {
	#check if swap is available
    echo
    echo "* Creating Swap File. Please wait..."
    if [ $(free | awk '/^Swap:/ {exit !$2}') ] || [ ! -f "/var/mnode_swap.img" ];then
	    echo -e "${GREEN}* No proper swap, creating it.${NONE}";
	    # needed because ant servers are ants
	    sudo rm -f /var/mnode_swap.img &>> ${SCRIPT_LOGFILE}
	    sudo dd if=/dev/zero of=/var/mnode_swap.img bs=1024k count=${SWAPSIZE} &>> ${SCRIPT_LOGFILE}
	    sudo chmod 0600 /var/mnode_swap.img &>> ${SCRIPT_LOGFILE}
	    sudo mkswap /var/mnode_swap.img &>> ${SCRIPT_LOGFILE}
	    sudo swapon /var/mnode_swap.img &>> ${SCRIPT_LOGFILE}
	    echo '/var/mnode_swap.img none swap sw 0 0' | sudo tee -a /etc/fstab &>> ${SCRIPT_LOGFILE}
	    echo 'vm.swappiness=10' | sudo tee -a /etc/sysctl.conf &>> ${SCRIPT_LOGFILE}
	    echo 'vm.vfs_cache_pressure=50' | sudo tee -a /etc/sysctl.conf &>> ${SCRIPT_LOGFILE}
	else
	    echo -e "${GREEN}* All good, we have a swap.${NONE}";
	fi
}

function installFail2Ban() {
    echo
    echo -e "* Installing fail2ban. Please wait..."
    sudo apt-get -y install fail2ban &>> ${SCRIPT_LOGFILE}
    sudo systemctl enable fail2ban &>> ${SCRIPT_LOGFILE}
    sudo systemctl start fail2ban &>> ${SCRIPT_LOGFILE}
    # Add Fail2Ban memory hack if needed
    if ! grep -q "ulimit -s 256" /etc/default/fail2ban; then
       echo "ulimit -s 256" | sudo tee -a /etc/default/fail2ban &>> ${SCRIPT_LOGFILE}
       sudo systemctl restart fail2ban &>> ${SCRIPT_LOGFILE}
    fi
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

function installFirewall() {
    echo
    echo -e "* Installing UFW. Please wait..."
    sudo apt-get -y install ufw &>> ${SCRIPT_LOGFILE}
    sudo ufw allow OpenSSH &>> ${SCRIPT_LOGFILE}
    sudo ufw allow ${COINPORT}/tcp &>> ${SCRIPT_LOGFILE}
    sudo ufw allow ${COINRPCPORT}/tcp &>> ${SCRIPT_LOGFILE}
    if [ "${DNSPORT}" != "" ] ; then
        sudo ufw allow ${DNSPORT}/tcp &>> ${SCRIPT_LOGFILE}
        sudo ufw allow ${DNSPORT}/udp &>> ${SCRIPT_LOGFILE}
    fi
    echo "y" | sudo ufw enable &>> ${SCRIPT_LOGFILE}
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

function installDependencies() {
    echo
    echo -e "* Installing dependencies. Please wait..."
    sudo timedatectl set-ntp no &>> ${SCRIPT_LOGFILE}
    sudo apt-get install git ntp nano wget curl software-properties-common -y &>> ${SCRIPT_LOGFILE}
    if [[ -r /etc/os-release ]]; then
        . /etc/os-release
        if [[ "${VERSION_ID}" = "16.04" ]]; then
            wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb &>> ${SCRIPT_LOGFILE}
            sudo dpkg -i packages-microsoft-prod.deb &>> ${SCRIPT_LOGFILE}
            sudo apt-get install apt-transport-https -y &>> ${SCRIPT_LOGFILE}
            sudo apt-get update -y &>> ${SCRIPT_LOGFILE}
            sudo apt-get install dotnet-sdk-2.2 -y &>> ${SCRIPT_LOGFILE}
            echo -e "${NONE}${GREEN}* Done${NONE}";
        fi
        if [[ "${VERSION_ID}" = "18.04" ]]; then
            wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb &>> ${SCRIPT_LOGFILE}
            sudo dpkg -i packages-microsoft-prod.deb &>> ${SCRIPT_LOGFILE}
            sudo add-apt-repository universe -y &>> ${SCRIPT_LOGFILE}
            sudo apt-get install apt-transport-https -y &>> ${SCRIPT_LOGFILE}
            sudo apt-get update -y &>> ${SCRIPT_LOGFILE}
            sudo apt-get install dotnet-sdk-2.2 -y &>> ${SCRIPT_LOGFILE}
            echo -e "${NONE}${GREEN}* Done${NONE}";
        fi
        else
        echo -e "${NONE}${RED}* Version: ${VERSION_ID} not supported.${NONE}";
    fi
}

function compileWallet() {
    echo
    echo -e "* Compiling wallet. Please wait, this might take a while to complete..."
    cd /home/${NODE_USER}/
    git clone ${COINGITHUB} code &>> ${SCRIPT_LOGFILE}
    cd /home/${NODE_USER}/code
    git submodule update --init --recursive &>> ${SCRIPT_LOGFILE}
    cd ${COINDSRC}
    dotnet publish -c ${CONF} -r ${ARCH} -v m -o ${COINDLOC} &>> ${SCRIPT_LOGFILE}	   ### compile & publish code
    rm -rf /home/${NODE_USER}/code &>> ${SCRIPT_LOGFILE} 	   ### Remove source
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

function installWallet() {
    echo
    echo -e "* Installing wallet. Please wait..."
    cd /home/${NODE_USER}/
    echo -e "#!/bin/bash\nexport DOTNET_CLI_TELEMETRY_OPTOUT=1\ncd $COINDLOC\n$COINRUNCMD" > ${COINSTARTUP}
    echo -e "[Unit]\nDescription=${COINDAEMON}\nAfter=network-online.target\n\n[Service]\nType=simple\nUser=${NODE_USER}\nGroup=${NODE_USER}\nExecStart=${COINSTARTUP}\nRestart=always\nRestartSec=5\nPrivateTmp=true\nTimeoutStopSec=60s\nTimeoutStartSec=5s\nStartLimitInterval=120s\nStartLimitBurst=15\n\n[Install]\nWantedBy=multi-user.target" >${COINSERVICENAME}.service
    chown -R ${NODE_USER}:${NODE_USER} ${COINSERVICELOC} &>> ${SCRIPT_LOGFILE}
    sudo mv ${COINSERVICENAME}.service ${COINSERVICELOC} &>> ${SCRIPT_LOGFILE}
    sudo chmod 777 ${COINSTARTUP} &>> ${SCRIPT_LOGFILE}
    sudo systemctl --system daemon-reload &>> ${SCRIPT_LOGFILE}
    sudo systemctl enable ${COINSERVICENAME} &>> ${SCRIPT_LOGFILE}
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

function configureWallet() {
    echo
    echo -e "* Configuring wallet. Please wait..."
    cd /home/${NODE_USER}/
    RPCUSER=`cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1`
    RPCPASS=`cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1`
    sudo mkdir -p ${COINCORE}
    echo -e "externalip=${NODE_IP}\ntxindex=1\nlisten=1\ndaemon=1\nmaxconnections=64\nserver=1\nrpcuser=${RPCUSER}\nrpcpassword=${RPCPASS}\nrpcbind=${RPCBIND}\nrpcport=${COINRPCPORT}\nrpcallowip=${RPCBIND}" > ${COINCONFIG}
    sudo mv ${COINCONFIG} ${COINCORE}
    echo -e "${NONE}${GREEN}* Done${NONE}";
}

function startWallet() {
    echo
    echo -e "* Starting wallet daemon...${COINSERVICENAME}"
    sudo service ${COINSERVICENAME} start &>> ${SCRIPT_LOGFILE}
    sleep 2
    echo -e "${GREEN}* Done${NONE}";
}
function stopWallet() {
    echo
    echo -e "* Stopping wallet daemon...${COINSERVICENAME}"
    sudo service ${COINSERVICENAME} stop &>> ${SCRIPT_LOGFILE}
    sleep 2
    echo -e "${GREEN}* Done${NONE}";
}

function installUnattendedUpgrades() {
    echo
    echo "* Installing Unattended Upgrades..."
    sudo apt install unattended-upgrades -y &>> ${SCRIPT_LOGFILE}
    sleep 3
    sudo sh -c 'echo "Unattended-Upgrade::Allowed-Origins {" >> /etc/apt/apt.conf.d/50unattended-upgrades'
    sudo sh -c 'echo "        "${distro_id}:${distro_codename}";" >> /etc/apt/apt.conf.d/50unattended-upgrades'
    sudo sh -c 'echo "        "${distro_id}:${distro_codename}-security";" >> /etc/apt/apt.conf.d/50unattended-upgrades'
    sudo sh -c 'echo "APT::Periodic::AutocleanInterval "7";" >> /etc/apt/apt.conf.d/20auto-upgrades'
    sudo sh -c 'echo "APT::Periodic::Unattended-Upgrade "1";" >> /etc/apt/apt.conf.d/20auto-upgrades'
    cat /etc/apt/apt.conf.d/20auto-upgrades &>> ${SCRIPT_LOGFILE}
    echo -e "${GREEN}* Done${NONE}";
}

installMongodDB() {
    echo
	echo -e "* Installing MongoDB. Please wait..."
	sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv 9DA31620334BD75D9DCB49F368818C72E52529D4 &>> ${SCRIPT_LOGFILE}
	if [[ -r /etc/os-release ]]; then
	. /etc/os-release
		if [[ "${VERSION_ID}" = "16.04" ]]; then
			echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu xenial/mongodb-org/4.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-4.0.list &>> ${SCRIPT_LOGFILE}
		fi
		if [[ "${VERSION_ID}" = "18.04" ]]; then
			echo "deb [ arch=amd64 ] https://repo.mongodb.org/apt/ubuntu bionic/mongodb-org/4.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-4.0.list &>> ${SCRIPT_LOGFILE}
		fi
	fi
	sudo apt-get update &>> ${SCRIPT_LOGFILE}
	sudo apt-get install -y mongodb-org &>> ${SCRIPT_LOGFILE}
	echo "mongodb-org hold" | sudo dpkg --set-selections &>> ${SCRIPT_LOGFILE}
	echo "mongodb-org-server hold" | sudo dpkg --set-selections &>> ${SCRIPT_LOGFILE}
	echo "mongodb-org-shell hold" | sudo dpkg --set-selections &>> ${SCRIPT_LOGFILE}
	echo "mongodb-org-mongos hold" | sudo dpkg --set-selections &>> ${SCRIPT_LOGFILE}
	echo "mongodb-org-tools hold" | sudo dpkg --set-selections &>> ${SCRIPT_LOGFILE}
	sudo service mongod start &>> ${SCRIPT_LOGFILE}
	sleep 10
	echo -e "${NONE}${GREEN}* Done${NONE}";
	
	MONGOPASS=`cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1`
	echo "Mongo password used: ${MONGOPASS}" &>> ${SCRIPT_LOGFILE}
	mongo explorerdb --eval 'db.createUser( { user: "iquidus", pwd: "${MONGOPASS}", roles: [ "readWrite" ] } );'
}

installNginx() {
    echo
	echo -e "* Installing nginx. Please wait..."
	sudo apt-get -y install nginx &>> ${SCRIPT_LOGFILE}
	sudo service nginx start &>> ${SCRIPT_LOGFILE}
	sudo rm /etc/nginx/sites-available/default &>> ${SCRIPT_LOGFILE}
	sudo echo -e "server {\n    listen        80;\n    location / {\n        proxy_pass         http://localhost:3001;\n        proxy_http_version 1.1;\n        proxy_set_header   Upgrade \$http_upgrade;\n        proxy_set_header   Connection keep-alive;\n        proxy_set_header   Host \$host;\n        proxy_cache_bypass \$http_upgrade;\n        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;\n        proxy_set_header   X-Forwarded-Proto \$scheme;\n    }\n}" > /etc/nginx/sites-available/default
	sudo ufw allow 80 &>> ${SCRIPT_LOGFILE}
	sudo nginx -s reload
	sleep 5
	echo -e "${NONE}${GREEN}* Done${NONE}";
}

installExplorer() {
    echo
	echo -e "* Installing nodejs, npm & Iquidus Explorer. "
	cd /home/${NODE_USER}
	
	sudo apt-get update
	sudo apt install -y -qq nodejs-legacy &>> ${SCRIPT_LOGFILE}
	sudo apt-get install -y -qq npm &>> ${SCRIPT_LOGFILE}
	sudo apt-get install -qq &>> ${SCRIPT_LOGFILE}

	git clone https://github.com/RedstonePlatform/explorer /home/${NODE_USER}/explorer &>> ${SCRIPT_LOGFILE}
	cd /home/${NODE_USER}/explorer && npm install --production &>> ${SCRIPT_LOGFILE}

	cp settings.json.template settings.json &>> ${SCRIPT_LOGFILE}
	sudo sed -i -e 's/COINX/'"${COIN}"'/g' settings.json
	sudo sed -i -e 's/RPCUSERX/'"${RPCUSER}"'/g' settings.json
	sudo sed -i -e 's/RPCPASSX/'"${RPCPASS}"'/g' settings.json
	sudo sed -i -e 's/RPCPORTX/'"${COINRPCPORT}"'/g' settings.json
	sudo sed -i -e 's/MONGOUSERX/'"iquidus"'/g' settings.json
	sudo sed -i -e 's/MONGOPASSX/'"${MONGOPASS}"'/g' settings.json
	sudo sed -i -e 's/IPSTACK_APIKEY/'"${IPSTACK_APIKEY}"'/g' settings.json

	cd /home/${NODE_USER}
	sudo echo -e "#!/bin/bash\ncd /home/${NODE_USER}/explorer/\nnpm start" > /home/${NODE_USER}/iquidus.sh 
	sudo chmod +x iquidus.sh &>> ${SCRIPT_LOGFILE}

	sudo echo -e "[Unit]\nDescription=Redstone Iquidus Block Explorer\nAfter=network-online.target\n\n[Service]\nWorkingDirectory=/home/${NODE_USER}/explorer\nExecStart=/home/${NODE_USER}/iquidus.sh\nRestart=always\n\nTimeoutSec=10\nRestartSec=35\n[Install]\nWantedBy=multi-user.target\n" > /etc/systemd/system/iquidus@${NODE_USER}.service

	sudo systemctl --system daemon-reload &>> ${SCRIPT_LOGFILE}
	sudo systemctl enable iquidus@${NODE_USER} &>> ${SCRIPT_LOGFILE}
	sudo systemctl start iquidus@${NODE_USER} &>> ${SCRIPT_LOGFILE}
	sleep 2
	echo -e "${NONE}${GREEN}* Done${NONE}";	
}

displayServiceStatus() {
	echo
	echo
	on="${GREEN}ACTIVE${NONE}"
	off="${RED}OFFLINE${NONE}"

	if systemctl is-active --quiet redstoned@${NODE_USER}; then echo -e "Redstone Service: ${on}"; else echo -e "Redstone Service: ${off}"; fi
	if systemctl is-active --quiet iquidus@${NODE_USER}; then echo -e "Explorer Service: ${on}"; else echo -e "Explorer Service: ${off}"; fi
	if systemctl is-active --quiet mongod; then echo -e "Mongo Service   : ${on}"; else echo -e "Mongo Service   : ${off}"; fi
	if systemctl is-active --quiet nginx; then echo -e "nginx Service   : ${on}"; else echo -e "nginx Service   : ${off}"; fi
}

displayCompleteMessage() {
    echo
    echo -e "${GREEN} Installation complete. ${NONE}"
    echo -e "${NONE} The log file can be found here: ${SCRIPT_LOGFILE}${NONE}"
    echo -e "${NONE} Please ensure to check the service journal as follows:"
    echo -e "${NONE} Redstone Node   : ${PURPLE}journalctl -f -u ${COINSERVICENAME}${NONE}"
    echo -e "${NONE} Explorer Service: ${PURPLE}journalctl -f -u iquidus@${NODE_USER}${NONE}"
	echo -e "${RED} Please sync your node and then perform initial indexing by issuing the following commands: "
	echo -e "${RED} 1. cd ~/explorer && node scripts/sync.js index reindex"
	echo -e "${RED} 2. cd ~/explorer && node scripts/sync.js index update"
	echo -e "${RED} 3. Once completed, add the following to crontab to automate updates"
	echo -e "${RED}    */1 * * * * root cd /home/${NODE_USER}/explorer && /usr/bin/nodejs scripts/sync.js index update > /dev/null 2>&1"
	echo -e "${RED}    */5 * * * * root cd /home/${NODE_USER}/explorer && /usr/bin/nodejs scripts/peers.js > /dev/null 2>&1"
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
echo -e "${PURPLE}*    ${NONE}This script will install and configure your Redstone node,      ${PURPLE}*"
echo -e "${PURPLE}*    ${NONE}including Mongo & Iquidus Block Explorer                        ${PURPLE}*"
echo -e "${PURPLE}*    ${NONE}on Mainnet or Testnet.  Upgrade updates the node only!          ${PURPLE}*"
echo -e "${PURPLE}**********************************************************************${NONE}"
echo -e "${BOLD}"

check_root

echo -e "${BOLD}"
read -p " Do you want to setup on Mainnet (m), Testnet (t) or upgrade (u) your Redstone node. (m/t/u)?" response

if [[ "$response" =~ ^([mM])+$ ]]; then
    setMainVars
    read -p " Do you want to setup your Redstone node as a DNS Server (y/n)?" response
    echo -e "${NONE}"
    if [[ "$response" =~ ^([yY])+$ ]]; then
        setDNSVars
    fi
    setGeneralVars
    echo -e "${BOLD} The log file can be monitored here: ${SCRIPT_LOGFILE}${NONE}"
    echo -e "${BOLD}"
    checkOSVersion
    updateAndUpgrade
    create_user
    setupSwap
    installFail2Ban
    installFirewall
    installDependencies
    compileWallet
    installWallet
    configureWallet
    installUnattendedUpgrades
    startWallet
    installMongodDB
    installNginx
    installExplorer
    set_permissions
    displayServiceStatus
	displayCompleteMessage
 else
    if [[ "$response" =~ ^([tT])+$ ]]; then
        setTestVars
        read -p " Do you want to setup your Redstone node as a DNS Server (y/n)?" response
        echo -e "${NONE}"
        if [[ "$response" =~ ^([yY])+$ ]]; then
           setDNSVars
        fi
        setGeneralVars
        echo -e "${BOLD} The log file can be monitored here: ${SCRIPT_LOGFILE}${NONE}"
        echo -e "${BOLD}"
        checkOSVersion
        updateAndUpgrade
        create_user
        setupSwap
        installFail2Ban
        installFirewall
        installDependencies
        compileWallet
        installWallet
        configureWallet 
        installUnattendedUpgrades
        startWallet
        installMongodDB
        installNginx
        installExplorer		
        set_permissions
        displayServiceStatus
		displayCompleteMessage
 else
    if [[ "$response" =~ ^([uU])+$ ]]; then
        check_root
        ##TODO: Test for servicefile and only upgrade as required 
        ##TODO: Setup for testnet - test if file exists
        ##[ ! -f ${COINSERVICELOC}$COINSERVICENAME.service ] << Test for service file
        #Stop Test Service
        setTestVars
        setGeneralVars
        stopWallet
	    updateAndUpgrade
        compileWallet
        #Stop Main Service
        setMainVars
        setGeneralVars
        stopWallet
        compileWallet
        #Start Test Service
        setTestVars
        setGeneralVars
        startWallet
        #Start Main Service
        setMainVars
        setGeneralVars
        startWallet
    else
      echo && echo -e "${RED} Installation cancelled! ${NONE}" && echo
    fi
  fi
fi

echo -e "${GREEN} thecrypt0hunter 2019${NONE}"
cd ~

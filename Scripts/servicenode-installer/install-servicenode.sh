#!/bin/bash
# =================== YOUR DATA ========================
#bash <( curl -s https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Scripts/servicenode-installer/servicenode-installer/install-servicenode.sh )

SERVER_IP=$(curl --silent ipinfo.io/ip)
COINSERVICEINSTALLER="https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Scripts/servicenode-installer/servicenode-installer/install-coin.sh"
COINSERVICECONFIG="https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Scripts/servicenode-installer/servicenode-installer/config-redstone.sh"
USER=redstone-testnet #redstone
fork=redstone

if [ "$(id -u)" != "0" ]; then
    echo -e "${RED}* Sorry, this script needs to be run as root. Do \"sudo su root\" and then re-run this script${NONE}"
    exit 1
    echo -e "${NONE}${GREEN}* All Good!${NONE}";
fi

read -p "Mainnet (m), Testnet (t) or Upgrade (u)? " net

case $net in
         m)
            COINCORE=/home/${USER}/.${FORK}node/${FORK}/RedstoneMain
            ;;
         t)
            COINCORE=/home/${USER}/.${FORK}node/${FORK}/RedstoneTest
            ;;
        u)
           COINCORE=""
           ;;
         *)
           echo "$net is an invalid response."
           exit
           ;;
esac

# ================= SERVICE NODE SETTINGS ===========================
apiport=38222
date_stamp="$(date +%y-%m-%d-%s)"
logfile="/tmp/log_$date_stamp_output.log"
WalletName="servicenode-wallet"
WalletSecretWords=""
WalletPassword=""
WalletPassphrase=""
ServiceNodeAddress=""

### Install Coins Service
wget ${COINSERVICEINSTALLER} -O /home/${USER}/install-coin.sh
wget ${COINSERVICECONFIG} -O /home/${USER}/config-redstone.sh
chmod +x /home/${USER}/install-coin.sh
/home/${USER}/install-coin.sh -f ${fork} -n

### Wallet setup ####

######## Get some information from the user about the wallet ############
clear
echo -e "###########################################################################"
echo -e "####################### SERVICENODE WALLET CREATION #######################"
echo -e "###########################################################################"
echo
echo -e "Please enter the details about your servicenode wallet."
echo 
read -p "Name (default=servicenode-wallet):" response
if [[ "$response" != "" ]] ; then 
   WalletName="$response" 
fi
read -p "Password:" WalletPassword
read -p "Passphrase:" WalletPassword
echo 

### Setup the hot wallet
echo -e "*Creating your Service Node wallet ... please wait."

### grab a 12 word mneumonic
WalletSecretWords=$(sed -e 's/^"//' -e 's/"$//' <<<$(curl -sX GET "http://localhost:$apiport/api/Wallet/mnemonic?language=english&wordCount=12" -H "accept: application/json")) 

### create the wallet
curl -sX POST "http://localhost:$apiport/api/Wallet/recover" -H  "accept: application/json" -H  "Content-Type: application/json-patch+json" -d "{  \"mnemonic\": \"$WalletSecretWords\",  \"password\": \"$WalletPassword\",  \"passphrase\": \"$WalletPassphrase\",  \"name\": \"$WalletName\",  \"creationDate\": \"2019-01-01T07:33:09.051Z\"}" &>> ${logfile}

### Get the address
ServiceNodeAddress=$(sed -e 's/^"//' -e 's/"$//' <<<$(curl -sX GET "http://localhost:$apiport/api/Wallet/unusedaddress?WalletName=$WalletName&AccountName=account 0" -H  "accept: application/json"))
ServiceNodeAddress=${ServiceNodeAddress:12:34}

### Stop the Daemon & add the service node configuration
service ${USER}d@${USER} stop

### Inject or add servicenode details to redstone.conf file 
#sed -i "s/^\(servicenode.ipv4\).*/\1${SERVER_IP}/" ${COINCORE}/${FORK}.conf
#sed -i "s/^\(servicenode.port\).*/\1${COINSNPORT}/" ${COINCORE}/${FORK}.conf
#sed -i "s/^\(servicenode.ecdsakeyaddress\).*/\1${COINSNPORT}/" ${COINCORE}/${FORK}.conf

sudo sh -c 'echo "" >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "#### Redstone ServiceNode registration settings ####" >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "servicenode.ipv4=127.0.0.1" >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "#servicenode.ipv6=" >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "#servicenode.onion=" >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "servicenode.port=37123  >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "# Value of each registration transaction output (in satoshi)" >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "servicenode.txfeevalue=11000" >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "#service.url=" >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "#service.rsakeyfile=" >> ${COINCORE}/${FORK}.conf'
sudo sh -c 'echo "service.ecdsakeyaddress=${ServiceNodeAddress}" >> ${COINCORE}/${FORK}.conf'

## Pause to fund the service node address
read -p "Please send collateral to the Service Node address (then press a key): $ServiceNodeAddress" anyKey

### Restart the Daemon
service ${USER}d@${USER} start

echo
echo -e "Here's all the Service Node wallet details - keep this information safe offline, otherwise your funds are at risk:"
echo
echo -e "Name: " $WalletName
echo -e "Password: " $WalletPassword
echo -e "Passphrase: " $WalletPassphrase
echo -e "Mnemonic: " $WalletSecretWords
echo -e "Service Node address : ${RED}" $ServiceNodeAddress
echo -e "${NONE}"
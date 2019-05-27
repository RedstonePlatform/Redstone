#!/bin/bash
# =================== YOUR DATA ========================
#bash <( curl -s https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Scripts/servicenode-installer/install-servicenode.sh )

SERVER_IP=$(curl --silent ipinfo.io/ip)
COINSERVICEINSTALLER="https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Scripts/servicenode-installer/install-coin.sh"
COINSERVICECONFIG="https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Scripts/servicenode-installer/config-redstone.sh"
COINSNPORT=37123
USER=redstone-testnet #redstone
apiport=38222
fork=redstone

clear
if [ "$(id -u)" != "0" ]; then
    echo -e "${RED}* Sorry, this script needs to be run as root. Do \"sudo su root\" and then re-run this script${NONE}"
    exit 1
    echo -e "${NONE}${GREEN}* All Good!${NONE}";
fi

read -p "Mainnet (m), Testnet (t) or Upgrade (u)? " net

case $net in
         m)
            COINCORE=/home/${USER}/.${fork}node/${fork}/RedstoneMain
            ;;
         t)
            COINCORE=/home/${USER}/.${fork}node/${fork}/RedstoneTest
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
date_stamp="$(date +%y-%m-%d-%s)"
logfile="/tmp/log_$date_stamp_output.log"
WalletName="servicenode-wallet"
WalletSecretWords=""
WalletPassword=""
WalletPassphrase=""
ServiceNodeAddress=""

### Install Coins Service
wget --quiet ${COINSERVICEINSTALLER} -O ~/install-coin.sh
wget --quiet ${COINSERVICECONFIG} -O ~/config-redstone.sh
chmod +x ~/install-coin.sh
~/install-coin.sh -f ${fork} -n ${net}

### Wallet setup ####

######## Get some information from the user about the wallet ############
echo -e "####################### SERVICENODE WALLET CREATION #######################"
echo
echo -e "You are going to create your servicenode wallet - we need some information first."
echo 
read -p "Name (default=servicenode-wallet): " response
if [[ "$response" != "" ]] ; then 
   WalletName="$response" 
fi
read -p "Password: " WalletPassword
read -p "Passphrase: " WalletPassphrase
echo 

### Setup the hot wallet
echo -e "*Creating your Service Node wallet ... please wait."

### grab a 12 word mneumonic
WalletSecretWords=$(sed -e 's/^"//' -e 's/"$//' <<<$(curl -sX GET "http://localhost:$apiport/api/Wallet/mnemonic?language=english&wordCount=12" -H "accept: application/json")) 

### create the wallet
curl -sX POST "http://localhost:$apiport/api/Wallet/recover" -H  "accept: application/json" -H  "Content-Type: application/json-patch+json" -d "{  \"mnemonic\": \"$WalletSecretWords\",  \"password\": \"$WalletPassword\",  \"passphrase\": \"$WalletPassphrase\",  \"name\": \"$WalletName\",  \"creationDate\": \"2019-01-01T07:33:09.051Z\"}" &>> ${logfile}

### Get the address
ServiceNodeAddress=$(sed -e 's/^"//' -e 's/"$//' <<<$(curl -sX GET "http://localhost:$apiport/api/Wallet/unusedaddress?WalletName=$WalletName&AccountName=account%200" -H  "accept: application/json"))

### Stop the Daemon & add the service node configuration
service ${fork}d@${USER} stop

### Inject or add servicenode details to redstone.conf file 
sed -i "s/^\(servicenode.ipv4\).*/\1${SERVER_IP}/" ${COINCORE}/${fork}.conf
sed -i "s/^\(servicenode.port\).*/\1${COINSNPORT}/" ${COINCORE}/${fork}.conf
sed -i "s/^\(servicenode.txfeevalue\).*/\111000/" ${COINCORE}/${fork}.conf
sed -i "s/^\(servicenode.ecdsakeyaddress\).*/\1${ServiceNodeAddress}}/" ${COINCORE}/${fork}.conf

sudo sh -c "echo '####Service Node Settings####' >> ${COINCORE}/${fork}.conf"
sudo sh -c "echo 'servicenode.ipv4=${SERVER_IP}' >> ${COINCORE}/${fork}.conf"
sudo sh -c "echo '#servicenode.ipv6=' >> ${COINCORE}/${fork}.conf"
sudo sh -c "echo '#servicenode.onion=' >> ${COINCORE}/${fork}.conf"
sudo sh -c "echo 'servicenode.port=37123' >> ${COINCORE}/${fork}.conf"
sudo sh -c "echo '# Value of each registration transaction output (in satoshi)' >> ${COINCORE}/${fork}.conf"
sudo sh -c "echo 'servicenode.txfeevalue=11000' >> ${COINCORE}/${fork}.conf"
sudo sh -c "echo '#service.url=' >> ${COINCORE}/${fork}.conf"
sudo sh -c "echo '#service.rsakeyfile=' >> ${COINCORE}/${fork}.conf"
sudo sh -c "echo 'service.ecdsakeyaddress=${ServiceNodeAddress}' >> ${COINCORE}/${fork}.conf"

## Pause to fund the service node address
read -p "Please send collateral to the Service Node address: ${ServiceNodeAddress} (then press a key): " anyKey

### Restart the Daemon
echo -e "*Starting your Service Node now ... please wait."

service ${fork}d@${USER} start

##TODO: Wait for collateral to land using a loop around >> curl -X GET "http://localhost:38222/api/BlockStore/getaddressbalance?address=TH3VEB716PzWMYKLNrAT7fT3XDgmw7Wmey&minConfirmations=10" -H  "accept: application/json"

### Register Servicenode
sleep 60
curl -SX POST "http://localhost:$apiport/api/ServiceNode/register?WalletName=${WalletName}&AccountName=account%200&Password=${WalletPassword}" -H  "accept: application/json" &>> ${logfile}

echo
echo -e "Here's all the Service Node wallet details - keep this information safe offline, otherwise your funds are at risk:"
echo
echo -e "Name:       "$WalletName
echo -e "Password:   "$WalletPassword
echo -e "Passphrase: "$WalletPassphrase
echo -e "Mnemonic:   "$WalletSecretWords
echo -e "Service Node address : "$ServiceNodeAddress
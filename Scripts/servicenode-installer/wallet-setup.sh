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

##### Define Variables ######
apiport=38222
date_stamp="$(date +%y-%m-%d-%s)"
logfile="/tmp/log_$date_stamp_output.log"
WalletName="servicenode-wallet"
WalletSecretWords=""
WalletPassword=""
WalletPassphrase=""
ServiceNodeAddress=""

######## Get some information from the user about the wallet ############
clear
echo -e "${RED}${BOLD}#############################################################################${NONE}"
echo -e "${RED}${BOLD}######################## SERVICENODE WALLET  CREATION #######################${NONE}"
echo -e "${RED}${BOLD}#############################################################################${NONE}"
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

echo -e "${GREEN}Done.${NONE}"
echo
echo
echo -e "Here's all the Service Node wallet details - keep this information safe offline, otherwise your funds are at risk:"
echo
echo -e "Name      	:" $WalletName
echo -e "Password  	:" $WalletPassword
echo -e "Passphrase	:" $WalletPassphrase
echo -e "Mnemonic  	:" $WalletSecretWords
echo -e "Hot address     :${RED}" $ServiceNodeAddress
echo -e "${NONE}"

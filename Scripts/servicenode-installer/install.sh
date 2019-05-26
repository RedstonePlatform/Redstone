#!/bin/bash
# =================== YOUR DATA ========================
WEBSERVERBASHFILE="bash <( curl -s https://raw.githubusercontent.com/trustaking/server-install/master/install.sh )"
SERVER_IP=$(curl --silent ipinfo.io/ip)

COINSERVICEINSTALLER="https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Scripts/servicenode-installer/install-fork.sh"
COINSERVICECONFIG="https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Scripts/servicenode-installer/config-redstone.sh"
WALLETSETUP="https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Scripts/servicenode-installer/wallet-setup.sh"
USER=redstone-testnet #redstone

### Install Coins Service
wget ${COINSERVICEINSTALLER} -O /home/${USER}/install-servicenode.sh
wget ${COINSERVICECONFIG} -O /home/${USER}/config-redstone.sh
chmod +x /home/${USER}/install-${fork}.sh
/home/${USER}/install-$fork.sh -f ${fork}

### Wallet setup
bash <( curl -s ${WALLETSETUP} )

### Add the service node configuration

service ${USER}d@${USER) stop

### Inject servicenode details (SNwalletname, SNpassword, SNAddress ipaddress, ) into redstone.conf file 

#sed -i "s/^\(\$ticker='\).*/\1$fork';/" /home/${USER}/${SERVER_NAME}/include/config.php
#sed -i "s/^\(\$api_port='\).*/\1$apiport';/" /home/${USER}/${SERVER_NAME}/include/config.php

## Inject apiport into /scripts/trustaking-*.sh files
#sed -i "s/^\(apiport=\).*/\1$apiport/" /home/${USER}/${SERVER_NAME}/scripts/trustaking-cold-wallet-add-funds.sh
#sed -i "s/^\(apiport=\).*/\1$apiport/" /home/${USER}/${SERVER_NAME}/scripts/trustaking-cold-wallet-balance.sh
#sed -i "s/^\(apiport=\).*/\1$apiport/" /home/${USER}/${SERVER_NAME}/scripts/trustaking-cold-wallet-setup.sh
#sed -i "s/^\(apiport=\).*/\1$apiport/" /home/${USER}/${SERVER_NAME}/scripts/trustaking-cold-wallet-withdraw-funds.sh
#sed -i "s/^\(apiport=\).*/\1$apiport/" /home/${USER}/${SERVER_NAME}/scripts/hot-wallet-setup.sh

service ${USER}d@${USER) start



# Redstone-Node-Install
**Redstone Full Node**

An automated script to install a Redstone Node on various platforms.

To install a Redstone Node on Ubuntu 16.04 - as <code>sudo su root</code> run the following command:

<code>bash <( curl https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Redstone/Scripts/Node-Installer/install_redstone_node.sh )</code>

To install a  Node on a Raspberry Pi running Raspian - as <code>sudo su root</code> run the following:

<code>bash <( curl https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Redstone/Scripts/Node-Installer/install_redstone_RPI.sh )</code>

If you get the error "bash: curl: command not found", run this first: <code>apt-get -y install curl</code>

To install a Node on CentOS - as <code>sudo su root</code> run the following:

<code>bash <( curl https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Redstone/Scripts/Node-Installer/install_redstone_CentOS.sh )</code>

**Redstone Full Node with Explorer**

In addition to the Redstone Full Node installs MongoDB, Nako Block Chain Indexer, nginx and Stratis.Guru Block Explorer.

<code>bash <( curl https://raw.githubusercontent.com/RedstonePlatform/Redstone/master/Redstone/Scripts/Node-Installer/install_redstone_explorer_node.sh )</code>


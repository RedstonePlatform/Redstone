using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using NBitcoin;
using Newtonsoft.Json;
using Redstone.ServiceNode.Utils;

namespace Redstone.ServiceNode.Models
{
    /*
        Bitstream format for Redstone ServiceNode registration token

        A registration token, once submitted to the network, remains valid indefinitely until invalidated.

        The registration token for a Redstone ServiceNode server can be invalidated by the sending of a subsequent
        token transaction at a greater block height than the original. It is the responsibility of the Redstone
        client software to scan the blockchain for the most current server registrations prior to initiating
        contact with any server.

        The registration token consists of a single transaction broadcast on the network of choice (e.g. Redstone mainnet/testnet). 
        This transaction has any number of funding inputs, as normal.
        It has precisely one nulldata output marking the entire transaction as a Redstone ServiceNode registration.
        There can be an optional change return output, which if present MUST be at the start or end of the entire output list,
        the registration token outputs should occur in one contiguous block.
        
        The remainder of the transaction outputs are of near-dust value. Each output encodes 64 bytes of token data
        into a public key script. The contents and format of the encoded data is described below.

        The presumption is that the transaction outputs are not reordered by the broadcasting node.

        - OP_RETURN transaction output
        -> 31 bytes - Literal string: REDSTONE_SN_REGISTRATION_MARKER

        - Encoded public key transaction outputs
        -> 1 byte - Protocol version byte (255 = test registration to be ignored by mainnet wallets)
        -> 2 bytes - Length of registration header
        -> 34 bytes - Server ID of the service node (base58 representation of the collateral address, right padded with spaces)
        -> 4 bytes - IPV4 address of service node server; 00000000 indicates non-IPV4
        -> 16 bytes - IPV6address of service node server; 00000000000000000000000000000000 indicates non-IPV6
        -> 16 bytes - Onion (Tor) address of service node server; 00000000000000000000000000000000 indicates non-Tor
        -> 2 bytes - IPV4/IPV6/Onion TCP port of server
        -> 2 bytes - RSA signature length
        -> n bytes - RSA signature proving ownership of the Redstone ServiceNode server's private key (to prevent spoofing)
        -> 2 bytes - ECDSA signature length
        -> n bytes - ECDSA signature proving ownership of the Redstone ServiceNode server's private key
        -> 40 bytes - Hash of the service node server's configuration file
        <...>
        -> Protocol does not preclude additional data being appended in future without breaking compatibility

        On connection with the Redstone ServiceNode server by a client, the public key of the server will be verified
        by the client to ensure that the server is authentic and in possession of the registered keys. 
    */

    public class RegistrationToken
    {
        public static string Marker => "REDSTONE_SN_REGISTRATION_MARKER";

        public static string MarkerHex => "6a1f52454453544f4e455f534e5f524547495354524154494f4e5f4d41524b4552";
        //"6a1a425245455a455f524547495354524154494f4e5f4d41524b4552"
        //public static string MarkerHex => "a91473f8bb02cbc5b07968e3ebde6a9c68a527aaa01787";

        public int ProtocolVersion { get; set; }

        public string ServerId { get; set; }

        [JsonConverter(typeof(IPAddressConverter))]
        public IPAddress Ipv4Addr { get; set; }

        [JsonConverter(typeof(IPAddressConverter))]
        public IPAddress Ipv6Addr { get; set; }

        public string OnionAddress { get; set; }
        public int Port { get; set; }

        public byte[] RsaSignature { get; set; }
        public byte[] EcdsaSignature { get; set; }

        public string ConfigurationHash { get; set; }

        [JsonConverter(typeof(PubKeyConverter))]
        public PubKey EcdsaPubKey { get; set; }

        public RegistrationToken(int protocolVersion, string serverId, IPAddress ipv4Addr, IPAddress ipv6Addr, string onionAddress, string configurationHash, int port, PubKey ecdsaPubKey)
        {
            this.ProtocolVersion = protocolVersion;
            this.ServerId = serverId;
            this.Ipv4Addr = ipv4Addr;
            this.Ipv6Addr = ipv6Addr;
            this.OnionAddress = onionAddress;
            this.Port = port;
            this.ConfigurationHash = configurationHash;
            this.EcdsaPubKey = ecdsaPubKey;
        }

        public RegistrationToken()
        {
            // Constructor for when a token is being reconstituted from blockchain data
        }

        public List<byte> GetHeaderBytes()
        {
            var token = new List<byte>();

            token.AddRange(Encoding.ASCII.GetBytes(this.ServerId.PadRight(34)));

            if (this.Ipv4Addr != null)
            {
                token.AddRange(this.Ipv4Addr.GetAddressBytes());
            }
            else
            {
                token.Add(0x00); token.Add(0x00); token.Add(0x00); token.Add(0x00);
            }

            if (this.Ipv6Addr != null)
            {
                token.AddRange(this.Ipv6Addr.GetAddressBytes());
            }
            else
            {
                token.Add(0x00); token.Add(0x00); token.Add(0x00); token.Add(0x00);
                token.Add(0x00); token.Add(0x00); token.Add(0x00); token.Add(0x00);
                token.Add(0x00); token.Add(0x00); token.Add(0x00); token.Add(0x00);
                token.Add(0x00); token.Add(0x00); token.Add(0x00); token.Add(0x00);
            }

            if (this.OnionAddress != null)
            {
                token.AddRange(Encoding.ASCII.GetBytes(this.OnionAddress));
            }
            else
            {
                token.Add(0x00); token.Add(0x00); token.Add(0x00); token.Add(0x00);
                token.Add(0x00); token.Add(0x00); token.Add(0x00); token.Add(0x00);
                token.Add(0x00); token.Add(0x00); token.Add(0x00); token.Add(0x00);
                token.Add(0x00); token.Add(0x00); token.Add(0x00); token.Add(0x00);
            }

            // TODO: Review the use of BitConverter for endian-ness issues
            byte[] portNumber = BitConverter.GetBytes(this.Port);

            token.Add(portNumber[0]);
            token.Add(portNumber[1]);

            return token;
        }

        public byte[] GetRegistrationTokenBytes(RsaKey rsaKey, BitcoinSecret privateKeyEcdsa)
        {
            var token = GetHeaderBytes();

            var cryptoUtils = new CryptoUtils(rsaKey, privateKeyEcdsa);

            // Sign header (excluding preliminary length marker bytes) with RSA
            this.RsaSignature = cryptoUtils.SignDataRSA(token.ToArray());
            byte[] rsaLength = BitConverter.GetBytes(this.RsaSignature.Length);

            // Sign header (excluding preliminary length marker bytes) with ECDSA
            this.EcdsaSignature = cryptoUtils.SignDataECDSA(token.ToArray());
            byte[] ecdsaLength = BitConverter.GetBytes(this.EcdsaSignature.Length);

            // TODO: Check if the lengths are >2 bytes. Should not happen
            // for most conceivable signature schemes at current key lengths
            token.Add(rsaLength[0]);
            token.Add(rsaLength[1]);
            token.AddRange(this.RsaSignature);

            token.Add(ecdsaLength[0]);
            token.Add(ecdsaLength[1]);
            token.AddRange(this.EcdsaSignature);

            // Server configuration hash
            token.AddRange(Encoding.ASCII.GetBytes(this.ConfigurationHash));

            byte[] ecdsaPubKeyLength = BitConverter.GetBytes(this.EcdsaPubKey.ToBytes().Length);
            token.Add(ecdsaPubKeyLength[0]);
            token.Add(ecdsaPubKeyLength[1]);
            token.AddRange(this.EcdsaPubKey.ToBytes());

            // Finally add protocol byte and computed length to beginning of header
            byte[] protocolVersionByte = BitConverter.GetBytes(this.ProtocolVersion);
            byte[] headerLength = BitConverter.GetBytes(token.Count);

            token.Insert(0, protocolVersionByte[0]);
            token.Insert(1, headerLength[0]);
            token.Insert(2, headerLength[1]);

            return token.ToArray();
        }

        public static bool HasMarker(Transaction tx)
        {
            return GetMarkerIndex(tx) != -1;
        }

        private static int GetMarkerIndex(Transaction tx)
        {
            var markerIndex = -1;

            if (tx.Outputs.Count > 2)
            {
                // Find the nulldata transaction marker and validate
                for (int i = 0; i < tx.Outputs.Count; i++)
                {
                    if (tx.Outputs[i].ScriptPubKey.ToHex().ToLower() == MarkerHex)
                    {
                        markerIndex = i;
                        break;
                    }
                }
            }

            return markerIndex;
        }

        public void ParseTransaction(Transaction tx, Network network)
        {
            var markerIndex = GetMarkerIndex(tx);

            if (markerIndex == -1)
                throw new Exception("Missing Redstone registration marker from first transaction output");

            // Peek at first non-nulldata address to get the length information,
            // this indicates how many outputs have been used for encoding, and
            // by extension indicates if there will be a change address output

            PubKey[] tempPubKeyArray = tx.Outputs[markerIndex + 1].ScriptPubKey.GetDestinationPublicKeys(network);

            if (tempPubKeyArray.Length > 1)
                // This can't have been generated by a server registration, we don't use
                // multiple signatures for the registration transaction outputs by design
                throw new Exception("Registration transaction output has too many PubKeys");

            byte[] secondOutputData = BlockChainDataConversions.PubKeyToBytes(tempPubKeyArray[0]);

            var protocolVersion = (int)secondOutputData[0];

            var headerLength = (secondOutputData[2] << 8) + secondOutputData[1];

            // 64 = number of bytes we can store per output
            int numPubKeys = headerLength / 64;

            // Is there a partially 'full' PubKey holding the remainder of the bytes?
            if (headerLength % 64 != 0)
                numPubKeys++;

            if (tx.Outputs.Count < numPubKeys + 1)
                throw new Exception("Too few transaction outputs, registration transaction incomplete");

            PubKey[] tempPK;
            var pubKeyList = new List<PubKey>();
            for (int i = markerIndex + 1; i < numPubKeys + markerIndex + 1; i++)
            {
                tempPK = tx.Outputs[i].ScriptPubKey.GetDestinationPublicKeys(network);

                if (tempPK.Length > 1)
                    // This can't have been generated by a server registration, we don't use
                    // multiple signatures for the registration transaction outputs by design
                    throw new Exception("Registration transaction output has too many PubKeys");

                pubKeyList.Add(tempPK[0]);
            }

            byte[] bitstream = BlockChainDataConversions.PubKeysToBytes(pubKeyList);

            // Need to consume X bytes at a time off the bitstream and convert them to various
            // data types, then set member variables to the retrieved values.

            // Skip over protocol version and header length bytes
            int position = 3;
            this.ProtocolVersion = protocolVersion;

            byte[] serverIdTemp = GetSubArray(bitstream, position, 34);

            this.ServerId = Encoding.ASCII.GetString(serverIdTemp);

            position += 34;

            // Either a valid IPv4 address, or all zero bytes
            bool allZeroes = true;
            byte[] ipv4temp = GetSubArray(bitstream, position, 4);

            for (int i = 0; i < ipv4temp.Length; i++)
            {
                if (ipv4temp[i] != 0)
                    allZeroes = false;
            }

            if (!allZeroes)
            {
                this.Ipv4Addr = new IPAddress(ipv4temp);
            }
            else
            {
                this.Ipv4Addr = IPAddress.None;
            }

            position += 4;

            // Either a valid IPv6 address, or all zero bytes
            allZeroes = true;
            byte[] ipv6temp = GetSubArray(bitstream, position, 16);

            for (int i = 0; i < ipv6temp.Length; i++)
            {
                if (ipv6temp[i] != 0)
                    allZeroes = false;
            }

            if (!allZeroes)
            {
                this.Ipv6Addr = new IPAddress(ipv6temp);
            }
            else
            {
                this.Ipv6Addr = IPAddress.IPv6None;
            }

            position += 16;

            // Either a valid onion address, or all zero bytes
            allZeroes = true;
            byte[] onionTemp = GetSubArray(bitstream, position, 16);

            for (int i = 0; i < onionTemp.Length; i++)
            {
                if (onionTemp[i] != 0)
                    allZeroes = false;
            }

            if (!allZeroes)
            {
                this.OnionAddress = Encoding.ASCII.GetString(onionTemp);
            }
            else
            {
                this.OnionAddress = null;
            }

            position += 16;

            var temp = GetSubArray(bitstream, position, 2);
            this.Port = (temp[1] << 8) + temp[0];
            position += 2;

            temp = GetSubArray(bitstream, position, 2);
            var rsaLength = (temp[1] << 8) + temp[0];
            position += 2;

            this.RsaSignature = GetSubArray(bitstream, position, rsaLength);
            position += rsaLength;

            temp = GetSubArray(bitstream, position, 2);
            var ecdsaLength = (temp[1] << 8) + temp[0];
            position += 2;

            this.EcdsaSignature = GetSubArray(bitstream, position, ecdsaLength);
            position += ecdsaLength;

            byte[] configurationHashTemp = GetSubArray(bitstream, position, 40);
            this.ConfigurationHash = Encoding.ASCII.GetString(configurationHashTemp);
            position += 40;

            temp = GetSubArray(bitstream, position, 2);
            var ecdsaPubKeyLength = (temp[1] << 8) + temp[0];
            position += 2;

            this.EcdsaPubKey = new PubKey(GetSubArray(bitstream, position, ecdsaPubKeyLength));
            position += ecdsaPubKeyLength;

            // TODO: Validate signatures
        }

        public bool VerifySignatures()
        {
            if (this.EcdsaPubKey != null && this.EcdsaSignature != null)
                return this.EcdsaPubKey.VerifyMessage(GetHeaderBytes().ToArray(), Encoding.UTF8.GetString(this.EcdsaSignature));
            else
                return false;
        }

        public bool Validate(Network network)
        {
            if (this.EcdsaPubKey == null)
                return false;

            if (this.ServerId == null)
                return false;

            if (this.EcdsaPubKey.GetAddress(network).ToString() != this.ServerId)
                return false;

            if (this.Ipv4Addr == null && this.Ipv6Addr == null && this.OnionAddress == null)
                return false;

            if (!VerifySignatures())
                return false;

            // TODO: What other validation is required?
            return true;
        }

        private byte[] GetSubArray(byte[] data, int index, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public override bool Equals(object obj)
        {
            return obj is RegistrationToken token &&
                   this.ProtocolVersion == token.ProtocolVersion &&
                   this.ServerId == token.ServerId &&
                   EqualityComparer<IPAddress>.Default.Equals(this.Ipv4Addr, token.Ipv4Addr) &&
                   EqualityComparer<IPAddress>.Default.Equals(this.Ipv6Addr, token.Ipv6Addr) &&
                   this.OnionAddress == token.OnionAddress &&
                   this.Port == token.Port &&
                   EqualityComparer<byte[]>.Default.Equals(this.RsaSignature, token.RsaSignature) &&
                   EqualityComparer<byte[]>.Default.Equals(this.EcdsaSignature, token.EcdsaSignature) &&
                   this.ConfigurationHash == token.ConfigurationHash &&
                   EqualityComparer<PubKey>.Default.Equals(this.EcdsaPubKey, token.EcdsaPubKey);
        }

        public override int GetHashCode()
        {
            var hashCode = -77319232;
            hashCode = hashCode * -1521134295 + this.ProtocolVersion.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.ServerId);
            hashCode = hashCode * -1521134295 + EqualityComparer<IPAddress>.Default.GetHashCode(this.Ipv4Addr);
            hashCode = hashCode * -1521134295 + EqualityComparer<IPAddress>.Default.GetHashCode(this.Ipv6Addr);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.OnionAddress);
            hashCode = hashCode * -1521134295 + this.Port.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(this.RsaSignature);
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(this.EcdsaSignature);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.ConfigurationHash);
            hashCode = hashCode * -1521134295 + EqualityComparer<PubKey>.Default.GetHashCode(this.EcdsaPubKey);
            return hashCode;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
        -> 20 bytes - Hash of collateral pub key (also used as the server id)
        -> 20 bytes - Hash of reward pub key 
        -> 4 bytes - IPV4 address of service node server; 00000000 indicates non-IPV4
        -> 16 bytes - IPV6address of service node server; 00000000000000000000000000000000 indicates non-IPV6
        -> 16 bytes - Onion (Tor) address of service node server; 00000000000000000000000000000000 indicates non-Tor
        -> 2 bytes - IPV4/IPV6/Onion TCP port of server
        -> 4 bytes - Service Endpoint Uri length
        -> n bytes - Service Endpoint Uri
        -> 4 bytes - ECDSA signature length
        -> n bytes - ECDSA signature proving ownership of the Redstone ServiceNode server's private key
        <...>
        -> Protocol does not preclude additional data being appended in future without breaking compatibility

        On connection with the Redstone ServiceNode server by a client, the public key of the server will be verified
        by the client to ensure that the server is authentic and in possession of the registered keys. 
    */

    public class RegistrationToken
    {
        public static readonly int MAX_PROTOCOL_VERSION = 128; // >128 = regard as test versions
        public static readonly int MIN_PROTOCOL_VERSION = 1;

        public static string Marker => "REDSTONE_SN_REGISTRATION_MARKER";

        public static string MarkerHex => "6a1f52454453544f4e455f534e5f524547495354524154494f4e5f4d41524b4552";

        public int ProtocolVersion { get; set; }

        public string ServerId => this.CollateralPubKeyHash.ToString();

        [JsonConverter(typeof(PubKeyHashConverter))]
        public KeyId CollateralPubKeyHash { get; set; }

        [JsonConverter(typeof(PubKeyHashConverter))]
        public KeyId RewardPubKeyHash { get; set; }

        [JsonConverter(typeof(IPAddressConverter))]
        public IPAddress Ipv4Addr { get; set; }

        [JsonConverter(typeof(IPAddressConverter))]
        public IPAddress Ipv6Addr { get; set; }

        public string OnionAddress { get; set; }

        public int Port { get; set; }

        [JsonConverter(typeof(UriConverter))]
        public Uri ServiceEndpoint { get; set; }

        public byte[] EcdsaSignature { get; set; }

        [JsonConverter(typeof(PubKeyConverter))]
        public PubKey EcdsaPubKey { get; set; }


        public RegistrationToken(
            int protocolVersion,
            IPAddress ipv4Addr,
            IPAddress ipv6Addr,
            string onionAddress,
            int port,
            KeyId collateralPubKeyHash,
            KeyId rewardPubKeyHash,
            PubKey ecdsaPubKey,
            Uri serviceEndpoint)
        {
            this.ProtocolVersion = protocolVersion;
            this.Ipv4Addr = ipv4Addr;
            this.Ipv6Addr = ipv6Addr;
            this.OnionAddress = onionAddress;
            this.Port = port;
            this.CollateralPubKeyHash = collateralPubKeyHash;
            this.RewardPubKeyHash = rewardPubKeyHash;
            this.EcdsaPubKey = ecdsaPubKey;
            this.ServiceEndpoint = serviceEndpoint;
        }

        public RegistrationToken()
        {
            // Constructor for when a token is being reconstituted from blockchain data
        }

        public List<byte> GetHeaderBytes()
        {
            var token = new List<byte>();

            if (this.CollateralPubKeyHash != null)
            {
                var collateralPubKeyHashBytes = this.CollateralPubKeyHash.ToBytes();
                if (collateralPubKeyHashBytes.Length != 20)
                {
                    throw new Exception("CollateralPubKeyHash is invalid");
                }
                token.AddRange(collateralPubKeyHashBytes);
            }

            if (this.RewardPubKeyHash != null)
            {
                var rewardPubKeyHash = this.RewardPubKeyHash.ToBytes();
                if (rewardPubKeyHash.Length != 20)
                {
                    throw new Exception("CollateralPubKeyHash is invalid");
                }
                token.AddRange(rewardPubKeyHash);
            }

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

            var serviceEndpointBytes = Encoding.ASCII.GetBytes(this.ServiceEndpoint.ToString());
            byte[] serviceEndpointLength = BitConverter.GetBytes(serviceEndpointBytes.Length);

            token.AddRange(serviceEndpointLength);
            token.AddRange(serviceEndpointBytes);

            return token;
        }

        public byte[] GetRegistrationTokenBytes(BitcoinSecret ecsdaPrivateKey)
        {
            var token = GetHeaderBytes();

            // Sign header (excluding preliminary length marker bytes) with ECDSA
            this.EcdsaSignature = CryptoUtils.SignDataECDSA(token.ToArray(), ecsdaPrivateKey);
            byte[] ecdsaLength = BitConverter.GetBytes(this.EcdsaSignature.Length);

            token.AddRange(ecdsaLength);
            token.AddRange(this.EcdsaSignature);

            byte[] pubKeyLength = BitConverter.GetBytes(this.EcdsaPubKey.ToBytes().Length);
            token.Add(pubKeyLength[0]);
            token.Add(pubKeyLength[1]);
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

            byte[] collateralPubKeyHashTemp = GetSubArray(bitstream, position, 20);
            this.CollateralPubKeyHash = new KeyId(collateralPubKeyHashTemp);
            position += 20;

            byte[] rewaredPubKeyHashTemp = GetSubArray(bitstream, position, 20);
            this.RewardPubKeyHash = new KeyId(rewaredPubKeyHashTemp);
            position += 20;

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
            var servicEndpointLength = (temp[1] << 8) + temp[0];
            position +=4;

            var serviceEndpointTemp = GetSubArray(bitstream, position, servicEndpointLength);
            this.ServiceEndpoint = new Uri(Encoding.ASCII.GetString(serviceEndpointTemp));
            position += servicEndpointLength;

            temp = GetSubArray(bitstream, position, 2);
            var ecdsaLength = (temp[1] << 8) + temp[0];
            position += 4;

            this.EcdsaSignature = GetSubArray(bitstream, position, ecdsaLength);
            position += ecdsaLength;

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

            if (this.CollateralPubKeyHash == null)
                return false;

            if (this.RewardPubKeyHash == null)
                return false;

            if (this.Ipv4Addr == null && this.Ipv6Addr == null && this.OnionAddress == null)
                return false;

            if (this.ServiceEndpoint == null)
                return false;

            if (!VerifySignatures())
                return false;

            // Ignore protocol versions outside the accepted bounds
            if (this.ProtocolVersion < MIN_PROTOCOL_VERSION || this.ProtocolVersion > MAX_PROTOCOL_VERSION)
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
                   EqualityComparer<KeyId>.Default.Equals(this.CollateralPubKeyHash, token.CollateralPubKeyHash) &&
                   EqualityComparer<KeyId>.Default.Equals(this.RewardPubKeyHash, token.RewardPubKeyHash) &&
                   EqualityComparer<IPAddress>.Default.Equals(this.Ipv4Addr, token.Ipv4Addr) &&
                   EqualityComparer<IPAddress>.Default.Equals(this.Ipv6Addr, token.Ipv6Addr) &&
                   this.OnionAddress == token.OnionAddress &&
                   this.Port == token.Port &&
                   EqualityComparer<Uri>.Default.Equals(this.ServiceEndpoint, token.ServiceEndpoint) &&
                   EqualityComparer<byte[]>.Default.Equals(this.EcdsaSignature, token.EcdsaSignature) &&
                   EqualityComparer<PubKey>.Default.Equals(this.EcdsaPubKey, token.EcdsaPubKey);
        }

        public override int GetHashCode()
        {
            var hashCode = -77319232;
            hashCode = hashCode * -1521134295 + this.ProtocolVersion.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<KeyId>.Default.GetHashCode(this.CollateralPubKeyHash);
            hashCode = hashCode * -1521134295 + EqualityComparer<KeyId>.Default.GetHashCode(this.RewardPubKeyHash);
            hashCode = hashCode * -1521134295 + EqualityComparer<IPAddress>.Default.GetHashCode(this.Ipv4Addr);
            hashCode = hashCode * -1521134295 + EqualityComparer<IPAddress>.Default.GetHashCode(this.Ipv6Addr);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.OnionAddress);
            hashCode = hashCode * -1521134295 + this.Port.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Uri>.Default.GetHashCode(this.ServiceEndpoint);
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(this.EcdsaSignature);
            hashCode = hashCode * -1521134295 + EqualityComparer<PubKey>.Default.GetHashCode(this.EcdsaPubKey);
            return hashCode;
        }
    }
}

using NBitcoin;
using NBitcoin.DataEncoders;
using Redstone.Core.Policies;
using Stratis.Bitcoin.Networks;

namespace Redstone.Core.Networks
{
    public abstract class RedstoneBaseNetwork : StratisMain
    {
        /// <summary> Redstone maximal value for the calculated time offset. If the value is over this limit, the time syncing feature will be switched off. </summary>
        public const int RedstoneMaxTimeOffsetSeconds = 25 * 60;

        /// <summary> Redstone default value for the maximum tip age in seconds to consider the node in initial block download (2 hours). </summary>
        public const int RedstoneDefaultMaxTipAgeInSeconds = 2 * 60 * 60;

        /// <summary> The name of the root folder containing the different Redstone blockchains (RedstoneMain, RedstoneTest, RedstoneRegTest). </summary>
        public const string RedstoneRootFolderName = "redstone";

        /// <summary> The default name used for the Redstone configuration file. </summary>
        public const string RedstoneDefaultConfigFilename = "redstone.conf";

        protected void SetDefaults()
        {
            this.DefaultMaxOutboundConnections = 16;
            this.DefaultMaxInboundConnections = 109;
            this.MaxTipAge = RedstoneDefaultMaxTipAgeInSeconds;
            this.RootFolderName = RedstoneRootFolderName;
            this.DefaultConfigFilename = RedstoneDefaultConfigFilename;
            this.MaxTimeOffsetSeconds = RedstoneMaxTimeOffsetSeconds;
            this.MinTxFee = 10000;
            this.FallbackFee = 10000;
            this.MinRelayTxFee = 10000;

            this.GenesisVersion = 1;
            this.GenesisReward = Money.Zero;

            var encoder = new Bech32Encoder("bc");
            this.Bech32Encoders = new Bech32Encoder[2];
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.StandardScriptsRegistry = new RedstoneStandardScriptsRegistry();
        }

        protected void SetBase58Prefixes(byte[] pubKeyAddress, byte[] scriptAddress, byte[] secretKey)
        {
            this.Base58Prefixes = new byte[12][];
            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = pubKeyAddress;
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = scriptAddress;
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = secretKey;
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            this.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            this.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            this.Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2a };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };
            this.Base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };
        }

        protected void CreateRedstoneGenesisBlock(ConsensusFactory consensusFactory)
        {
            this.Genesis = CreateRedstoneGenesisBlock(consensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward);
        }

        protected static Block CreateRedstoneGenesisBlock(ConsensusFactory consensusFactory, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            string pszTimestamp = "http://www.escapistmagazine.com/news/view/109385-Computer-Built-in-Minecraft-Has-RAM-Performs-Division";
            Transaction txNew = consensusFactory.CreateTransaction();
            txNew.Version = 1;
            txNew.Time = nTime;
            txNew.AddInput(new TxIn()
            {
                ScriptSig = new Script(Op.GetPushOp(0), new Op()
                {
                    Code = (OpcodeType)0x1,
                    PushData = new[] { (byte)42 }
                }, Op.GetPushOp(Encoders.ASCII.DecodeData(pszTimestamp)))
            });
            txNew.AddOutput(new TxOut()
            {
                Value = genesisReward,
            });
            Block genesis = consensusFactory.CreateBlock();
            genesis.Header.BlockTime = Utils.UnixTimeToDateTime(nTime);
            genesis.Header.Bits = nBits;
            genesis.Header.Nonce = nNonce;
            genesis.Header.Version = nVersion;
            genesis.Transactions.Add(txNew);
            genesis.Header.HashPrevBlock = uint256.Zero;
            genesis.UpdateMerkleRoot();
            return genesis;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using NBitcoin.Networks;
using NBitcoin.Protocol;

namespace NBitcoin
{
    public partial class Network
    {
        public static Network RedstoneMain => Network.GetNetwork("RedstoneMain") ?? Register(new RedstoneMain());

        public static Network RedstoneTest => Network.GetNetwork("RedstoneTest") ?? Register(new RedstoneTest());


        protected static Block CreateRedstoneGenesisBlock(ConsensusFactory consensusFactory, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            string pszTimestamp = "http://www.bbc.co.uk/sport/football/43632830";
            return CreateRedstoneGenesisBlock(consensusFactory, pszTimestamp, nTime, nNonce, nBits, nVersion, genesisReward);
        }

        protected static Block CreateRedstoneGenesisBlock(ConsensusFactory consensusFactory, string pszTimestamp, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
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

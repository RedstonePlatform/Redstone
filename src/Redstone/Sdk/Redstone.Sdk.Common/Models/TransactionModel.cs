namespace Redstone.Sdk.Models
{
    public class TransactionModel
    {
        public string Hex { get; set; }
        public string Txid { get; set; }
        public long Size { get; set; }
        public long Version { get; set; }
        public long Locktime { get; set; }
        public Vin[] Vin { get; set; }
        public Vout[] Vout { get; set; }
        public string Blockhash { get; set; }
        public long Confirmations { get; set; }
        public long Time { get; set; }
        public long Blocktime { get; set; }
    }

    public class Vin
    {
        public string Txid { get; set; }
        public long Vout { get; set; }
        public ScriptSig ScriptSig { get; set; }
        public long Sequence { get; set; }
    }

    public class ScriptSig
    {
        public string Asm { get; set; }
        public string Hex { get; set; }
    }

    public class Vout
    {
        public double Value { get; set; }
        public long N { get; set; }
        public ScriptPubKey ScriptPubKey { get; set; }
    }

    public class ScriptPubKey
    {
        public string Asm { get; set; }
        public string Hex { get; set; }
        public long ReqSigs { get; set; }
        public string Type { get; set; }
        public string[] Addresses { get; set; }
    }

}

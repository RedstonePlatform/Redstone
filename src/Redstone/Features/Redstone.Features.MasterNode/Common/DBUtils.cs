// Based on NTumbleBit DBreezeRepository by Nicolas Dorier

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using DBreeze;
using Stratis.Bitcoin.Utilities.JsonConverters;

namespace Redstone.Features.ServiceNode.Common
{
    public class DBUtils : IDisposable
    {
        private string _Folder;
        public DBUtils(string folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            this._Folder = folder;
        }

        private Dictionary<string, DBreezeEngineReference> _EnginesByParitionKey = new Dictionary<string, DBreezeEngineReference>();

        public void UpdateOrInsert<T>(string partitionKey, string rowKey, T data, Func<T, T, T> update)
        {
            lock (this._EnginesByParitionKey)
            {
                var engine = GetEngine(partitionKey);
                using (var tx = engine.GetTransaction())
                {
                    T newValue = data;
                    var existingRow = tx.Select<string, byte[]>(GetTableName<T>(), rowKey);
                    if (existingRow != null && existingRow.Exists)
                    {
                        var existing = Serializer.ToObject<T>(Unzip(existingRow.Value));
                        if (existing != null)
                            newValue = update(existing, newValue);
                    }
                    tx.Insert(GetTableName<T>(), rowKey, Zip(Serializer.ToString(newValue)));
                    tx.Commit();
                }
            }
        }

        private byte[] Zip(string unzipped)
        {
            MemoryStream ms = new MemoryStream();
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
            {
                StreamWriter writer = new StreamWriter(gzip, Encoding.UTF8);
                writer.Write(unzipped);
                writer.Flush();
            }
            return ms.ToArray();
        }

        private string Unzip(byte[] bytes)
        {
            try
            {
                MemoryStream ms = new MemoryStream(bytes);
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    StreamReader reader = new StreamReader(gzip, Encoding.UTF8);
                    var unzipped = reader.ReadToEnd();
                    return unzipped;
                }
            }
            catch (InvalidDataException) //Temporary, old deployment have non zipped data
            {
                return Encoding.UTF8.GetString(bytes);
            }
        }

        private DBreezeEngine GetEngine(string partitionKey)
        {
            if (!Directory.Exists(this._Folder))
                Directory.CreateDirectory(this._Folder);
            string partitionPath = GetPartitionPath(partitionKey);
            if (!Directory.Exists(partitionPath))
                Directory.CreateDirectory(partitionPath);
            DBreezeEngineReference engine;
            if (!this._EnginesByParitionKey.TryGetValue(partitionKey, out engine))
            {
                engine = new DBreezeEngineReference() { PartitionKey = partitionKey, Engine = new DBreezeEngine(partitionPath) };
                this._EnginesByParitionKey.Add(partitionKey, engine);
                this._EngineReferences.Enqueue(engine);
            }
            engine.Used++;
            while (this._EngineReferences.Count > this.MaxOpenedEngine)
            {
                var reference = this._EngineReferences.Dequeue();
                reference.Used--;
                if (reference.Used <= 0 && reference != engine)
                {
                    if (this._EnginesByParitionKey.Remove(reference.PartitionKey))
                        reference.Engine.Dispose();
                }
                else
                {
                    this._EngineReferences.Enqueue(reference);
                }
            }
            return engine.Engine;
        }

        Queue<DBreezeEngineReference> _EngineReferences = new Queue<DBreezeEngineReference>();

        public int OpenedEngine
        {
            get
            {
                lock (this._EnginesByParitionKey)
                {
                    return this._EngineReferences.Count;
                }
            }
        }
        public int MaxOpenedEngine
        {
            get;
            set;
        } = 10;

        class DBreezeEngineReference
        {
            public DBreezeEngine Engine
            {
                get; set;
            }
            public string PartitionKey
            {
                get;
                internal set;
            }
            public int Used
            {
                get; set;
            }
        }
        private string GetPartitionPath(string partitionKey)
        {
            return Path.Combine(this._Folder, GetDirectory(partitionKey));
        }

        private string GetDirectory(string partitionKey)
        {
            return partitionKey;
        }

        public void Delete(string partitionKey)
        {
            lock (this._EnginesByParitionKey)
            {
                if (!this._EnginesByParitionKey.ContainsKey(partitionKey))
                    return;

                var engine = GetEngine(partitionKey);
                engine.Dispose();
                this._EnginesByParitionKey.Remove(partitionKey);
                NBitcoin.Utils.DeleteRecursivelyWithMagicDust(GetPartitionPath(partitionKey));
            }
        }

        public T[] List<T>(string partitionKey)
        {
            lock (this._EnginesByParitionKey)
            {
                List<T> result = new List<T>();
                var engine = GetEngine(partitionKey);
                using (var tx = engine.GetTransaction())
                {
                    foreach (var row in tx.SelectForward<string, byte[]>(GetTableName<T>()))
                    {
                        result.Add(Serializer.ToObject<T>(Unzip(row.Value)));
                    }
                }
                return result.ToArray();
            }
        }

        public Dictionary<TKey, TVal> GetDictionary<TKey, TVal>(string partitionKey)
        {
            lock (this._EnginesByParitionKey)
            {
                Dictionary<TKey, TVal> result = new Dictionary<TKey, TVal>();
                var engine = GetEngine(partitionKey);
                using (var tx = engine.GetTransaction())
                {
                    foreach (var row in tx.SelectForward<TKey, byte[]>(GetTableName<TKey>()))
                    {
                        result.Add(row.Key, Serializer.ToObject<TVal>(Unzip(row.Value)));
                    }
                }
                return result;
            }
        }

        private string GetTableName<T>()
        {
            return typeof(T).FullName;
        }

        public T Get<T>(string partitionKey, string rowKey)
        {
            lock (this._EnginesByParitionKey)
            {
                var engine = GetEngine(partitionKey);
                using (var tx = engine.GetTransaction())
                {
                    return Get<T>(rowKey, tx);
                }
            }
        }

        private T Get<T>(string rowKey, DBreeze.Transactions.Transaction tx)
        {
            var row = tx.Select<string, byte[]>(GetTableName<T>(), rowKey);
            if (row == null || !row.Exists)
                return default(T);
            return Serializer.ToObject<T>(Unzip(row.Value));
        }

        public bool Delete<T>(string partitionKey, string rowKey)
        {
            lock (this._EnginesByParitionKey)
            {
                bool removed = false;
                var engine = GetEngine(partitionKey);
                using (var tx = engine.GetTransaction())
                {
                    tx.RemoveKey(GetTableName<T>(), rowKey, out removed);
                    tx.Commit();
                }
                return removed;
            }
        }

        public void Dispose()
        {
            lock (this._EnginesByParitionKey)
            {
                foreach (var engine in this._EnginesByParitionKey)
                {
                    engine.Value.Engine.Dispose();
                }
                this._EngineReferences.Clear();
                this._EnginesByParitionKey.Clear();
            }
        }
    }
}
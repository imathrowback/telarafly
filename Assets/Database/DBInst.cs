using Assets.DatParser;
using Assets.RiftAssets;
using CGS;
using Ionic.Zlib;
using Mono.Data.Sqlite;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

namespace Assets.Database
{
    public delegate void ProgressCallback(string message);
    public delegate void LoadedCallback(DB db);
    public static class DBInst
    {
        private static System.Threading.Thread loadThread;
        static object lockObj = new object();

        public static CObject toObj(this DB db, long ds, long key)
        {
            entry e = db.getEntry(ds, key);
            MemoryStream str = new MemoryStream(e.decompressedData);
            return Parser.processStreamObject(str);
        }

        public static bool loaded = false;
        public static bool loading {  get { return loadThread.IsAlive; } }
        static private DBLang langdb;
        static private DB db;
        public static DB inst   { get {
                while (db == null) ;
                lock (lockObj)
                {
                    return db;
                }
            }
        }

        public static DBLang lang_inst
        {
            get
            {
                lock (lockObj)
                {
                    return langdb;
                }
            }
        }


        public static event ProgressCallback progress = delegate { };
        public static event LoadedCallback isloadedCallback = delegate { };

        /**
         * If the db is loaded, call the callback immediately, otherwise, register the callback
         */ 
        public static void loadOrCallback(LoadedCallback loadCallback)
        {
            lock (isloadedCallback)
            {
                if (db != null)
                    loadCallback.Invoke(db);
                else
                    isloadedCallback += loadCallback;
            }
        }

        static DBInst()
        {
            loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(loadDatabase_));
            loadThread.Start();
        }
        
        private static void loadDatabase_()
        {
            try
            {
                lock (lockObj)
                {
                    Debug.Log("get asset database inst");
                    AssetDatabase adb = AssetDatabaseInst.DB;
                    Debug.Log("get telara.db");
                    AssetEntry ae = adb.getEntryForFileName("telara.db");
                    Debug.Log("done get telara.db");

                    string entryHash = Util.bytesToHexString(ae.hash);

                    string namePath = System.IO.Path.GetTempPath() + "telaraflydb";
                    string compressedSQLDB = namePath + ".db3";
                    string dbHashname = namePath + ".hash";

                    AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                    {
                        if (origc != null)
                            origc.Close();
                    };

                    string foundHash = "";
                    Debug.Log("check telaradb hash");
                    if (File.Exists(dbHashname) && File.Exists(compressedSQLDB))
                    {
                        string[] lines = File.ReadAllLines(dbHashname);
                        if (lines.Length == 1)
                            foundHash = lines[0];
                    }

                    DB db;
                    if (!foundHash.Equals(entryHash))
                    {
                        db = readDB(adb.extract(ae), compressedSQLDB, (s) => { progress.Invoke("[Phase 1 of 2]" + s); });
                        File.WriteAllLines(dbHashname, new String[] { entryHash });
                    }
                    else
                    {
                        db = new DB();
                        processSQL(db, compressedSQLDB, (s) => { progress.Invoke("[Phase 1 of 2]" + s); });
                    }

                    progress.Invoke("[Phase 1 of 2] Reading language database");
                    langdb = new DBLang(adb, "english", (s) => { progress.Invoke("[Phase 1 of 2]" + s); });

                    DBInst.db = db;
                    if (db != null)
                    {
                        loaded = true;
                        lock (isloadedCallback)
                        {
                            isloadedCallback.Invoke(db);
                        }
                        Debug.Log("db and lang done");
                        progress.Invoke("");
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                progress.Invoke("Error while loading:" + ex);
                throw ex;
            }
        }
        private static DB readDB(byte[] telaraDBData, string outSQLDb, Action<String> progress)
        {
            Debug.Log("get new DB");

            DB db = new DB();

            try
            {
                byte[] key = System.Convert.FromBase64String("IoooW3zsQgm22XaVQ0YONAKehPyJqEyaoQ7sEqf1XDc=");

                BinaryReader reader = new BinaryReader(new MemoryStream(telaraDBData));
                Debug.Log("get page size");
                reader.BaseStream.Seek(16, SeekOrigin.Begin);
                UInt16 pageSize = (UInt16)IPAddress.NetworkToHostOrder(reader.readShort());
                Debug.Log("go page size:" + pageSize);

                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                MemoryStream decryptedStream = new MemoryStream();

                int pageCount = telaraDBData.Length / pageSize;
                for (int i = 1; i < pageCount + 1; i++)
                {
                    byte[] iv = getIV(i);
                    BufferedBlockCipher cipher = new BufferedBlockCipher(new OfbBlockCipher(new AesEngine(), 128));
                    ICipherParameters cparams = new ParametersWithIV(new KeyParameter(key), iv);
                    cipher.Init(false, cparams);

                    byte[] bdata = reader.ReadBytes(pageSize);
                    byte[] ddata = new byte[pageSize];
                    cipher.ProcessBytes(bdata, 0, bdata.Length, ddata, 0);
                    // bytes 16-23 on the first page are NOT encrypted, so we need to replace them once we decrypt the page
                    if (i == 1)
                        for (int x = 16; x <= 23; x++)
                            ddata[x] = bdata[x];
                    decryptedStream.Write(ddata, 0, ddata.Length);
                    progress.Invoke("Decoding db " + i + "/" + pageCount);
                }
                decryptedStream.Seek(0, SeekOrigin.Begin);

                File.WriteAllBytes(outSQLDb, decryptedStream.ToArray());
                processSQL(db, outSQLDb,  progress);
                Debug.Log("finished processing");
            }
            catch(Exception ex)
            {
                Debug.LogError(ex);
            }
            return db;
        }

        private static byte[] getIV(int i)
        {
            byte[] iv = new byte[16];
            MemoryStream str = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(str))
            {
                writer.Write((long)i);
                writer.Write(0L);
                writer.Flush();
                str.Seek(0, SeekOrigin.Begin);
                return str.ToArray();
            }
        }
        static SqliteConnection origc;

        static Dictionary<long, HuffmanReader> dsHuffmanreaders = new Dictionary<long, HuffmanReader>();

        private static byte[] getEntry(long id, long key)
        {
            string datasetQ = "select * from dataset where datasetId=" + id + " and datasetKey=" + key;
            using (SqliteCommand datasetQcmd = new SqliteCommand(datasetQ, origc))
            {
                using (SqliteDataReader reader = datasetQcmd.ExecuteReader(System.Data.CommandBehavior.Default))
                {
                    while (reader.Read())
                    {
                        byte[] compressedData = (byte[])reader.GetValue(5);

                        // cache the huffman readers to save having to compute the huffman trees every time and also we don't have to read the frequency tables every time
                        HuffmanReader huffreader;
                        if (!dsHuffmanreaders.TryGetValue(id, out huffreader))
                        {
                            huffreader = new HuffmanReader(getFreqData(origc, id));
                            dsHuffmanreaders[id] = huffreader;
                        }
                        return getData(compressedData, huffreader);
                    }
                }
            }
            Debug.LogError("Failed to get result for " + id + ":" + key);
            return new byte[0];
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }
        private static byte[] getData(byte[] compressedData, byte[] freq)
        {
            HuffmanReader reader = new HuffmanReader(freq);
            return getData(compressedData, reader);
        }
        private static byte[] getData(byte[] compressedData, HuffmanReader reader)
        {
            MemoryStream mdata = new MemoryStream(compressedData);
            int uncompressedSize = RiftAssets.Util.readUnsignedLeb128_X(mdata);
            byte[] dataOutput = new byte[uncompressedSize];

            MemoryStream compressedD = new MemoryStream();
            CopyStream(mdata, compressedD);
            compressedD.Seek(0, SeekOrigin.Begin);

            byte[] newCompressed = compressedD.ToArray();

            return reader.read(newCompressed, newCompressed.Length, uncompressedSize);
        }
        
        private static void processSQL(DB db, string compressedSQLDB,  Action<String> progress)
        {

            Debug.Log("Connect.");
            origc = new SqliteConnection("URI=file:" + compressedSQLDB);
            
            origc.Open();


            string datasetQ = "select * from dataset";
            string datasetCQ = "select count(*) from dataset";
            long totalCount = 0;
            using (SqliteCommand datasetQcmd = new SqliteCommand(datasetCQ, origc))
            {
                using (SqliteDataReader reader = datasetQcmd.ExecuteReader(System.Data.CommandBehavior.Default))
                {
                    reader.Read();
                    totalCount = reader.GetInt64(0);
                }
            }
            
            using (SqliteCommand datasetQcmd = new SqliteCommand(datasetQ, origc))
            {
                using (SqliteDataReader reader = datasetQcmd.ExecuteReader(System.Data.CommandBehavior.Default))
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        progress.Invoke(i + "/" + totalCount);
                        long datasetId = reader.GetInt64(0);
                        long datasetKey = reader.GetInt64(1);
                        string name = "";
                        if (!reader.IsDBNull(4))
                            name = reader.GetString(4);
                        entry e = new entry();
                        e.id = datasetId;
                        e.key = datasetKey;
                        e.name = name;
                        e.getData = getEntry;
                        db.Add(e);
                        i++;
                    }
                }
            }
        }


        static byte[] getFreqData(SqliteConnection c, long id)
        {
            try
            {
                using (SqliteCommand datasetQcmd = new SqliteCommand("select frequencies from dataset_compression where datasetId=" + id, c))
                {
                    using (SqliteDataReader reader = datasetQcmd.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                    {
                        if (reader.Read())
                        {
                            var obj = reader.GetValue(0);
                            return (byte[])obj;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
            return new byte[0];
        }

    }
}

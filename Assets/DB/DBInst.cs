using Assets.RiftAssets;
using CGS;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.DB
{
    /** TelaraDB  */
    class DBInst
    {
        static DB db;

        public static DB readDB(string expectedChecksum, Action<String> progress)
        {
            if (db != null)
                return db;
            GC.Collect();
            db = new DB();
            try
            {
                using (FileStream fs = new FileStream("dat.xmlz", FileMode.Open))
                {
                    using (ProgressStream ps = new ProgressStream(fs))
                    {
                        long total = fs.Length;
                        ps.BytesRead += (s, a) => progress.Invoke("Loading Database: " + (int)(((float)a.StreamPosition / (float)total) * 100.0) + " %");

                        using (DeflateStream ds = new DeflateStream(ps, CompressionMode.Decompress))
                        {
                            long pos = fs.Position;
                            //Debug.Log("begin read");
                            using (BinaryReader reader = new BinaryReader(ds))
                            {
                                db.dbchecksum = reader.ReadString();
                                string simpleHash = db.dbchecksum.Replace("-", string.Empty).ToLower();
                                if (!expectedChecksum.Equals(simpleHash))
                                {
                                    UnityEngine.Debug.Log("Checksum in file[" + db.dbchecksum + "] doesn't match expected from assets[" + expectedChecksum + "]");
                                    db = null;
                                    return null;
                                }
                                else
                                    UnityEngine.Debug.Log("Found existing database, let us use it!");

                                int count = reader.ReadInt32();
                                for (int i = 0; i < count; i++)
                                {
                                    entry e = new entry();
                                    e.id = reader.ReadInt64();
                                    e.key = reader.ReadInt64();
                                    e.name = reader.ReadString();
                                    e.decompressedData = new byte[reader.ReadInt32()];
                                    reader.Read(e.decompressedData, 0, e.decompressedData.Length);

                                    db.Add(e);
                                }
                            }
                            try
                            {


                            }
                            catch (Exception ex)
                            {
                                Debug.Log(ex);
                            }

                            //Debug.Log("done read");

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log("Unable to read existing database so we will recreate it:" + ex);
                db = null;
                return null;
            }
            return db;
        }

        private static void create(string assetManifest, string assetDir)
        {
            System.Diagnostics.Process pr;
            string file = @"decomp\tdbdecomp.exe";
            pr = new System.Diagnostics.Process();
            pr.StartInfo.FileName = file;
            pr.StartInfo.Arguments = "\"" + assetManifest + "\" \"" + assetDir + "\"";
            pr.Start();
            pr.WaitForExit();
        }
        internal static void createTelaraDBFromDB(AssetDatabase adb)
        {
            byte[] data = adb.extractUsingFilename("telara.db");
            string dbName = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(dbName, data);

                System.Diagnostics.Process pr;
                string file = @"decomp\tdbdecomp-norift.exe";
                pr = new System.Diagnostics.Process();
                pr.StartInfo.FileName = file;
                pr.StartInfo.Arguments = "\"" + dbName + "\"";
                pr.Start();
                pr.WaitForExit();
            }
            finally
            {
                File.Delete(dbName);
            }
        }
    }
}

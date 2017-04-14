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
                                if (!expectedChecksum.Equals(db.dbchecksum))
                                {
                                    db = null;
                                    UnityEngine.Debug.Log("Checksum in file doesn't match file from assets");
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
                                    //string dataStr = Encoding.UTF8.GetString(e.decompressedData);

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
                return null;
            }
            return db;
        }

        internal static void create(string assetManifest, string assetDir)
        {
            System.Diagnostics.Process pr;
            string file = @"decomp\tdbdecomp.exe";
            pr = new System.Diagnostics.Process();
            pr.StartInfo.FileName = file;
            pr.StartInfo.Arguments = "\"" + assetManifest + "\" \"" + assetDir + "\"";
            pr.Start();
            pr.WaitForExit();
        }
    }
}

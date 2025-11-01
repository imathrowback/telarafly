using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.RiftAssets
{
   public class AssetDatabaseLocal : AssetDatabase
    {

        public override bool isRemote()
        {
            return false;
        }
        private List<AssetFile> assets = new List<AssetFile>();
     

        public AssetDatabaseLocal( Manifest manifest) : base(manifest)
        {
        }


        public void add( AssetFile assetFile)
        {
            assets.Add(assetFile);
        }

        private List<AssetEntry> getEntries()
        {
            List<AssetEntry> entries = new List<AssetEntry>();
            foreach (AssetFile file in assets)
            {
                entries.AddRange(file.getEntries());
            }
            return entries;
        }

        private AssetFile findAssetFileForID(byte[] id)
        {
            return findAssetFileForID(Util.bytesToHexString(id));
        }

        Dictionary<string, List<AssetFile>> assetFiles;
        System.Object locko = new System.Object();
        internal string overrideDirectory;

        private AssetFile findAssetFileForID( string id)
        {
            if (assetFiles == null)
            {
                lock (locko)
                {
                    if (assetFiles == null)
                    {
                        assetFiles = new Dictionary<string, List<AssetFile>>();
                        foreach (AssetFile file in assets)
                        {
                            foreach (AssetEntry ae in file.getEntries())
                            {
                                List<AssetFile> list;
                                if (!assetFiles.TryGetValue(ae.strID, out list))
                                {
                                    list = new List<AssetFile>();
                                    assetFiles.Add(ae.strID, list);
                                }
                                list.Add(file);
                            }
                        }
                    }
                }
            }
            if (id == null)
                return null;
            List<AssetFile> holders;
            if (!assetFiles.TryGetValue(id, out holders))
                return null;

                //new List<AssetFile>();
            //foreach (AssetFile file in assets)
            //    if (file.contains(id))
            //        holders.Add(file);
            if (holders.Count == 0)
                return null;

            if (holders.Count > 1)
            {
                try
                {
                    // we have a 32 and 64 bit one pick the right one
                    AssetFile f_32 = (from f in holders where !f.is64 select f).First();
                    AssetFile f_64 = (from f in holders where f.is64 select f).First();
                    if (is64())
                        return f_64;
                    return f_32;
                }
                catch (Exception ex)
                {
                    //Debug.LogWarning(ex);
                    //string holdersStr = String.Join(",", holders.Select(x => x.file).ToArray());
                    //Debug.LogWarning("More than one asset file [" + holdersStr + "] contains id [" + id + "]");
                }
            }
            return holders[0];
        }


        private AssetEntry getEntryForFileName( string filename, RequestCategory requestCategory = RequestCategory.NONE)
        {

            string id = getID(filename, requestCategory);
            //Debug.Log("find asset file for id:" + id);
            AssetFile assetFile = findAssetFileForID(id);
            //Debug.Log("result:" + assetFile);
            if (assetFile == null)
            {
                throw new Exception(
                        "Filename found in manifest but unable to locate ID[" + id + "] in assets: '" + filename
                                + "'[" + Util.hashFileName(filename) + "]");
            }
            //Debug.Log("found with id:" + id);
            return assetFile.getEntry(id);
            
        }



        /** Attempt to extract the asset with the given filename */
        public override byte[] extractUsingFilename( string filename, RequestCategory requestCategory = RequestCategory.NONE)
        {
            if (overrideDirectory != null)
            {
                Debug.Log("override detected, try to find in override");

                string bfilename = Path.GetFileName(filename);
                string bhash = Util.hashFileName(bfilename);
                string overriddenFilename1 = overrideDirectory + Path.DirectorySeparatorChar + bfilename;
                string overriddenFilename2 = overrideDirectory + Path.DirectorySeparatorChar + bhash;

                if (File.Exists(overriddenFilename1))
                {
                    Debug.Log("read override file:" + filename + " => " + overriddenFilename1);
                    return File.ReadAllBytes(overriddenFilename1);
                }
                else if (File.Exists(overriddenFilename2))
                {
                    Debug.Log("read override file: " + filename + " => " + overriddenFilename2);
                    return File.ReadAllBytes(overriddenFilename2);
                }
                else
                {
                    Debug.Log("override file not found, get id");
                    string id = getID(filename, requestCategory);

                    Debug.Log("search override directory for [" + bhash + "] or [" + bfilename + "] or [" + id + "]");
                    foreach (String s in Directory.GetFiles(overrideDirectory))
                    {
                        
                        if (s.StartsWith(bhash + "-"))
                        {
                            Debug.Log("read override file: " + s);
                            return File.ReadAllBytes(s);
                        }
                        else
                            if (Path.GetFileName(s).StartsWith(bfilename) && s.EndsWith("B"))
                        {
                            Debug.Log("read override file: " + s);
                            return File.ReadAllBytes(s);
                        }
                        else if ((Path.GetFileName(s).Contains("-" + id + "-")))
                        {
                            Debug.Log("read override file: " + s);
                            return File.ReadAllBytes(s);
                        }
                    }
                }
                Debug.Log("failed to detect override");
            }

            Debug.Log("try extracting filename " + filename + " from existing assets");
            byte[] data = extract(getEntryForFileName(filename, requestCategory));
            if (true)
            {
                //File.WriteAllBytes(@"L:\RIFT_VIEW\data\" + filename, data);
            }
            return data;
        }

        private byte[] extract( AssetEntry ae)
        {
            return ae.file.extract(ae);
        }

        private byte[] extractPart( AssetEntry ae,  int size)
        {
            AssetFile af = ae.file;
            if (af != ae.file)
                throw new Exception("Incorrect af found for asset[" + ae + "]");
            return af.extractPart(ae, size, null, false);
        }

        internal string getHash(string v)
        {
            AssetEntry ae = getEntryForFileName(v);
            return BitConverter.ToString(ae.hash);
        }

        private void extract( AssetEntry ae,  Stream fos)
        {
            byte[] data = extract(ae);
            fos.Write(data, 0, data.Length);
            fos.Flush();
        }

        private AssetEntry getEntryForID( byte[] id)
        {
            AssetFile file = findAssetFileForID(id);
            if (file != null)
                return file.getEntry(id);
            return null;
        }

        private AssetFile getAssetFile( AssetEntry ae)
        {
            return ae.file;
        }

       
    }
}

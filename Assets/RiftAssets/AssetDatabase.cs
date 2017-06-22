using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.RiftAssets
{
   public class AssetDatabase
    {
        List<AssetFile> assets = new List<AssetFile>();
        Manifest manifest;
        bool is64;

        public AssetDatabase( Manifest manifest)
        {
            is64 = manifest.getIs64();
            this.manifest = manifest;
        }

        public Manifest getManifest()
        {
            return manifest;
        }

        public List<AssetFile> getAssetFiles()
        {
            return assets;
        }

        public void add( AssetFile assetFile)
        {
            assets.Add(assetFile);
        }

        public List<AssetEntry> getEntries()
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

        private AssetFile findAssetFileForID( string id)
        {
            if (id == null)
                return null;
            List<AssetFile> holders = new List<AssetFile>();
            foreach (AssetFile file in assets)
                if (file.contains(id))
                    holders.Add(file);
            if (holders.Count == 0)
                return null;

            if (holders.Count > 1)
            {
                // we have a 32 and 64 bit one pick the right one
                AssetFile f_32 = (from f in holders where !f.is64 select f).First();
                AssetFile f_64 = (from f in holders where f.is64 select f).First();
                if (is64)
                    return f_64;
                return f_32;
            }

            return holders[0];
        }
       
        public bool filenameExists( string filename)
        {
            return manifest.containsHash(Util.hashFileName(filename));
        }

        public enum RequestCategory
        {
            NONE,
            MAP,
            PHYSICS,
            TEXTURE,
            SHADER,
            SHADER_FORWARD,
            GEOMETRY,
            CHARACTER,
            PARTICLE,
            VFX,
            UIFONT,
            UIFLASH,
            MOVIE,
            AUDIO,
            PROPERTYCLASSDATA,
            GAMEDATA,
            ENGLISH,
            PATCH,
        }

        public AssetEntry getEntryForFileName( string filename, RequestCategory requestCategory = RequestCategory.NONE)
        {
            //Debug.Log("get entry for filename:" + filename + " with request category " + requestCategory);
            List<ManifestEntry> entries = manifest.getEntriesForFilenameHash(Util.hashFileName(filename));

            if (entries.Count() == 0)
                throw new Exception("Filename hash not found in manifest: '" + filename + "'");

            // strip out duplicate patch paks
            entries.RemoveAll(e => {
                return manifest.getPAKName(e.pakIndex).Contains("patch") && entries.Any(x => x != e && x.idStr.Equals(e.idStr));
            });

            //Debug.Log("found " + entries.Count() + " entries in manifest that match");
            string id = "";
            if (entries.Count() == 1)
            {
                // if there was only one result, then use it
                id = entries.First().idStr;
            }
            else
            {

                // otherwise, break the tie with a category test
                if (requestCategory == RequestCategory.NONE)
                {
                    Debug.LogError("tie for " + filename + " with no category set");
                    // we can't break a tie without a request category
                    String str = "";
                    foreach (ManifestEntry entry in entries)
                    {
                        str += "\t" + entry + " :" + manifest.getPAKName(entry.pakIndex) + "\n";
                    }
                        throw new Exception("Multiple ids match the filename [" + filename + "] but no request category was given, unable to determine which to return.\n" + str);
                }
                // work out which one we want based on the category
                string requestStr = requestCategory.ToString().ToLower();
                Debug.Log("multiple ids found, using request category " + requestStr);
                ManifestEntry finalEntry = null;
                foreach (ManifestEntry entry in entries)
                {
                    Debug.Log("[" + filename + "]: considering entry:" + entry + " :" + manifest.getPAKName(entry.pakIndex));
                    ManifestPAKFileEntry pak = manifest.getPAK(entry.pakIndex);
                    string pakName = pak.name;
                    if (pakName.Contains(requestStr))
                    {
                        finalEntry = entry;
                        break;
                    }
                }

                if (finalEntry == null)
                {
                    // if we were still unable to break the tie
                    Debug.LogError("tiebreak for " + filename + " no id match");

                    throw new Exception("Multiple ids match the filename [" + filename + "] but the request category[" + requestStr + "] did not match any, unable to determine which to return");
                }
                id = finalEntry.idStr;
                Debug.Log("settled on entry:" + finalEntry + " :" + manifest.getPAKName(finalEntry.pakIndex));

            }
            AssetFile assetFile = findAssetFileForID(id);
            if (assetFile == null)
                throw new Exception(
                        "Filename hash found in manifest but unable to locate ID[" + id + "] in assets: '" + filename
                                + "'");
            //Debug.Log("found with id:" + id);
            return assetFile.getEntry(id);
            
        }

        /** Attempt to extract the asset with the given filename */
        public byte[] extractUsingFilename( string filename, RequestCategory requestCategory = RequestCategory.NONE)
        {
            return extract(getEntryForFileName(filename, requestCategory));
        }

        /** Attempt to extract the asset with the given filename */
        public void extractToFilename( String filename,  String outputfilename)
        {
            try 
		{
                using (FileStream fos = new FileStream(outputfilename, FileMode.Truncate))
                {
                    using (BufferedStream bi = new BufferedStream(fos))
                    {
                        byte[] data = extract(getEntryForFileName(filename));
                        bi.Write(data, 0, data.Length);
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public byte[] extract( AssetEntry ae)
        {
            return ae.file.extract(ae);
        }

        public byte[] extractPart( AssetEntry ae,  int size)
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

        public void extract( AssetEntry ae,  Stream fos)
        {
            findAssetFileForID(ae.strID).extract(ae, fos);
        }

        public AssetEntry getEntryForID( byte[] id)
        {
            AssetFile file = findAssetFileForID(id);
            if (file != null)
                return file.getEntry(id);
            return null;
        }

        public AssetFile getAssetFile( AssetEntry ae)
        {
            return ae.file;
        }

       
    }
}

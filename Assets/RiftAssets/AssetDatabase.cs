using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


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

        private AssetFile findAssetFileForID( String id)
        {
            if (id == null)
                return null;
            List<AssetFile> holders = new List<AssetFile>();
            foreach (AssetFile file in assets)
                if (file.contains(id))
                    holders.Add(file);
            if (holders.Count == 0)
                return null;
            //if (holders.size() > 1)
            //	System.err.println("WARN: More than one asset file found containing id " + id);

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

        public AssetFile getAssetFileContainingFilename( String filename)
        {
            String id = Util.findIDAsStrInManifestForFileName(filename, manifest);
            if (id != null)
            {
                AssetFile assetFile = findAssetFileForID(id);
                return assetFile;
            }
            return null;

        }

        public AssetEntry getEntryForFileNameHash( String filenameHash)
        {
            String id = manifest.filenameHashToID(filenameHash);
            if (id == null)
                throw new Exception("Filename hash not found: '" + filenameHash + "'");
            AssetFile assetFile = findAssetFileForID(id);
            if (assetFile == null)
                throw new Exception(
                        "Filename hash found in manifest but unable to locate ID[" + id + "] in assets: '" + filenameHash
                                + "'");
            return assetFile.getEntry(id);
        }

        public bool filenameExists( String filename)
        {
            try
            {
                String id = Util.findIDAsStrInManifestForFileName(filename, manifest);
                return id != null;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public AssetEntry getEntryForFileName( String filename)
        {
            String id = Util.findIDAsStrInManifestForFileName(filename, manifest);
            if (id == null)
                throw new Exception("Filename hash not found: '" + filename + "'");
            AssetFile assetFile = findAssetFileForID(id);
            if (assetFile == null)
                throw new Exception(
                        "Filename hash found in manifest but unable to locate ID[" + id + "] in assets: '" + filename
                                + "'");
            return assetFile.getEntry(id);
        }

        /** Attempt to extract the asset with the given filename */
        public byte[] extractUsingFilename( String filename)
        {
            return extract(getEntryForFileName(filename));
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

        public bool containsFilenameHash( String filenameHash)
        {
            if (manifest.containsHash(filenameHash))
            {
                String id = manifest.filenameHashToID(filenameHash);
                return findAssetFileForID(id) != null;
            }
            return false;
        }
    }
}

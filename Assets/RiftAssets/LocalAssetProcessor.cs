using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace Assets.RiftAssets
{
    public class LocalAssetProcessor
    {
        public static AssetDatabase buildDatabase(Manifest manifest, String assetDirectory, string overrideDirectory)
        {
            AssetDatabaseLocal assets = new AssetDatabaseLocal(manifest);
            assets.overrideDirectory = overrideDirectory;
            Debug.Log("manifest64:" + manifest.getIs64());
            string[] files = Directory.GetFiles(assetDirectory);
            foreach (string file in files)
            {
                // ignore 32bit assets if using 64 bit manifest and vice versa
                if (manifest.getIs64() && file.Contains("assets32"))
                    continue;
                else if (!manifest.getIs64() && file.Contains("assets64"))
                    continue;
                try
                {
                    AssetFile af = buildAssetFileDatabase(file, manifest);
                    if (af != null)
                        assets.add(af);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            return assets;
        }


        private static AssetFile buildAssetFileDatabase(string file, Manifest manifest)
        {
            AssetFile assetFile = new AssetFile(file);

            using (BinaryReader dis = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                byte[] magic = dis.ReadBytes(4);
                string magicStr = System.Text.Encoding.Default.GetString(magic);
                if (!magicStr.Equals("TWAD"))
                {
                    throw new Exception("Invalid AssetFile:" + file + ": Invalid magic signature: " + magicStr);
                }
                int version = dis.ReadInt32();
                int headersize = dis.ReadInt32();
                int maxfiles = dis.ReadInt32();
                int files = dis.ReadInt32();

                //System.out.println(
                //		"Version:" + version + " headerSize:" + headersize + ", maxFiles:" + maxfiles + ", files:" + files);
                // System.out.println("\t assets " + files + ", max:" + maxfiles);
                // FIXME: For some reason, using the "files" variable doesnt read the proper amount of actual files, deleted? renamed? moved?
                int actualFiles = 0;
                for (int i = 0; i < maxfiles; i++)
                {
                    byte[] entry = dis.ReadBytes(44);

                    using (BinaryReader bis = new BinaryReader(new MemoryStream(entry)))
                    {
                        byte[] id = bis.ReadBytes(8);
                        int offset = bis.ReadInt32();
                        int size1 = bis.ReadInt32();
                        int size2 = bis.ReadInt32();
                        // not sure what size2 is, it is always the same as size1?

                        int filenum = bis.ReadInt16();
                        int flag = bis.ReadInt16(); //compressed?
                        byte[] hash = bis.ReadBytes(20);
                        int sizeD = manifest.getFileSize(Util.bytesToHexString(id));

                        if (offset == 0)
                        {
                            // entry was deleted? Corrupt? No longer exists?
                            //System.err.println(
                            //		"found zero offset entry for data entry " + i + ", entry:"
                            //				+ Util.bytesToHexString(entry) + " @" + streamOffset);
                        }
                        else
                        {
                            if (sizeD < 0)
                            {
                                Debug.LogWarning("ID[" + Util.bytesToHexString(id) + "] does not have an entry in the manifest: assetFile:" + file + "@" + offset + ", size:" + size1);
                                sizeD = size2;
                            }
                        assetFile.addAsset(new AssetEntry(id, offset, size1, sizeD, flag != 0, hash, assetFile));
                            actualFiles++;
                        }

                    }

                }
            }
            return assetFile;
        }

    }
}

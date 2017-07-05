using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace Assets.RiftAssets
{
    public class AssetProcessor
    {
        public static AssetDatabase buildDatabase(Manifest manifest, String assetDirectory)
        {
            AssetDatabase assets = new AssetDatabase(manifest);

            string[] files = Directory.GetFiles(assetDirectory);
            foreach (string file in files)
            {
                assets.add(buildAssetFileDatabase(file));
            }
            return assets;
        }


        private static AssetFile buildAssetFileDatabase(string file)
        {
            AssetFile assetFile = new AssetFile(file);

            using (BinaryReader dis = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                byte[] magic = dis.ReadBytes(4);
                //System.out.println(new String(magic));
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
                        int filenum = bis.ReadInt16();
                        int flag = bis.ReadInt16(); //compressed?
                        byte[] hash = bis.ReadBytes(20);

                        if (offset == 0)
                        {
                            // entry was deleted? Corrupt? No longer exists?
                            //System.err.println(
                            //		"found zero offset entry for data entry " + i + ", entry:"
                            //				+ Util.bytesToHexString(entry) + " @" + streamOffset);
                        }
                        else
                        {
                            assetFile.addAsset(new AssetEntry(id, offset, size1, size2, flag != 0, hash, assetFile));
                            actualFiles++;
                        }

                    }

                }
            }
            return assetFile;
        }

    }
}

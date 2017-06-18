using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.RiftAssets
{
    abstract public class AssetDatabase
    {
        abstract public Manifest getManifest();
        abstract public string getHash(String filename);
        abstract public byte[] extractUsingFilename(String filename);
        abstract public bool filenameExists(String filename);

        abstract public byte[] extractUsingHash(string fname);
    }
}

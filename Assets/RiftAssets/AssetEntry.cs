using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.RiftAssets
{
    public class AssetEntry
    {
        public AssetEntry( byte[] id,  int offset,  int size,  int sizeD,  bool compressed,
             byte[] hash,  AssetFile file)
        {
            this.id = id;
            strID = Util.bytesToHexString(id);
            this.sizeD = sizeD;
            this.offset = offset;
            this.size = size;
            this.compressed = compressed;
            this.hash = hash;
            this.file = file;
        }

         public AssetFile file;
         public byte[] hash;
         public int offset;
         public int size;
         public bool compressed;
         public byte[] id;
         public String strID;
         public int sizeD;
    }
}

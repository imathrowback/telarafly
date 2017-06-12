using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace Assets.RiftAssets
{
    public class PAKFileEntry
    {
        public string hash1Str;
        public string hash2Str;
        public string combHash;

        public PAKFileEntry(byte[] manifestData, BinaryReader dis2)
        {

            int offsetToName = dis2.readInt();
            name = readString(manifestData, offsetToName);
            fileSize1 = dis2.readInt();
            fileSize2 = dis2.readInt();
            compressionType = dis2.readByte();
            byte[] hash1 = new byte[20];
            dis2.read(hash1);
            byte[] hash2 = new byte[20];
            dis2.read(hash2);

            hash1Str = Util.bytesToHexString(hash1);
            hash2Str = Util.bytesToHexString(hash2);
            combHash = hash1Str + hash2Str;
            //System.out.println("\t" + Util.bytesToHexString(hash1) + ":" + Util.bytesToHexString(hash2));
        }

        private string readString(byte[] manifestData, int offset)
        {
            BinaryReader dis2 = new BinaryReader(new MemoryStream(manifestData));
            dis2.skip(offset);
            StringBuilder buff = new StringBuilder();
            int x = 0;
            do
            {
                x = dis2.readByte();

                if (x != 0)
                    buff.Append((char)x);
            } while (x != 0);
            return buff.ToString();
        }

        public override string ToString()
        {
            return String.Format("{0} size1: {1}, size2:{2}: compressionType:{3}",
                    name,
                    fileSize1,
                    fileSize2,
                    compressionType);

        }

        public int getSize()
        {
            if (compressionType == 0)
                return fileSize1;
            else
                return fileSize2;
        }

        public String name;
        public int fileSize1;
        public int fileSize2;
        public byte compressionType;
        public byte[] hash1;
        public byte[] hash2;
    }
}



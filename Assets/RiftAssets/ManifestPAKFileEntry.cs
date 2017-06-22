using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Linq;
using System.Text;
using Assets.DatParser;
namespace Assets.RiftAssets
{
    public class ManifestPAKFileEntry
    {
        public string hash1Str;
        public string hash2Str;
        public string combHash;

        public ManifestPAKFileEntry(byte[] manifestData, BinaryReader dis2)
        {

            int offsetToName = dis2.readInt();
            name = readString(manifestData, offsetToName);
            fileSize1 = dis2.readInt();
            fileSize2 = dis2.readInt();
            compressionType = dis2.readByte();
            byte[] hash1 = new byte[20];
            dis2.readFully(hash1);
            byte[] hash2 = new byte[20];
            dis2.readFully(hash2);

            hash1Str = Util.bytesToHexString(hash1);
            hash2Str = Util.bytesToHexString(hash2);
            combHash = hash1Str + hash2Str;
        }

        private string readString(byte[] manifestData, int offset)
        {
            BinaryReader dis2 = new BinaryReader(new MemoryStream(manifestData));
            dis2.ReadBytes(offset);
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


        override public string ToString()
        {
            return (String.Format(
                    "{0} size1: {1}, size2:{2}: compressionType:{3}",
                    name,
                    fileSize1,
                    fileSize2,
                    compressionType));

        }

        public int getSize()
        {
            if (compressionType == 0)
                return fileSize1;
            else
                return fileSize2;
        }

        public string name { get; }
        public int fileSize1 { get; }
        public int fileSize2 { get; }
        public byte compressionType { get; }
        public byte[] hash1 { get; }
        public byte[] hash2 { get; }
    }
}

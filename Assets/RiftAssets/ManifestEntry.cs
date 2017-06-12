using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace Assets.RiftAssets
{
    public class ManifestEntry
    {
        public string idStr;
        public string hashStr;
        public  byte[] id;
        public  byte[] filenameHash;
        public  int pakOffset;
        public  int compressedSize;
        public  int size;
        public  short pakIndex;
        public  short w2;
        public  short w3;
        public  int w4;
        public  int lang;
        public  byte[] shahash;
        public  int unk;
        public string shaStr;

        public ManifestEntry(BinaryReader  dis) 
        {
            // read the ID of the entry
            id = new byte[8];
		dis.readFully(id);

		// read the filename hash of the entry
		filenameHash = new byte[4];
		dis.readFully(filenameHash);
		Array.Reverse(filenameHash);

		// store the ID and filename hash into a map for easy lookup
		idStr = Util.bytesToHexString(id);
		hashStr = Util.bytesToHexString(filenameHash);

		pakOffset = dis.readInt();
		compressedSize = dis.readInt();
		size = dis.readInt();
		pakIndex = dis.readShort();

		//if (w1 > 2193)
		//	System.out.println(w1);
		w2 = dis.readShort();
		w3 = dis.readShort();
		w4 = dis.readByte();
		lang = dis.readByte();
		shahash = new byte[20];
		dis.readFully(shahash);
		unk = dis.readInt();
		shaStr = Util.bytesToHexString(shahash);
		//Date t = CFileTimeConvertor.readFileTime(dis);

	}

    
    override public string ToString()
    {
        return ("[namehash]" + hashStr + ":[partsha1sum]:" + idStr + ":[pakoffset]" +
                StringUtils.leftPad("" + pakOffset, 10, ' ') + ":[compressedSize]" +
                StringUtils.leftPad("" + compressedSize, 10, ' ') + ":[filesize]" + StringUtils.leftPad("" + size, 10, ' ')
                + ":"
                + "[PAKIndex]" + StringUtils.leftPad("" + pakIndex, 4, ' ') + ":[unkw2]"
                + StringUtils.leftPad("" + w2, 6, ' ') + ":[lang]" + lang + ""
                + ":[unk]" + unk
                + ":[hash]:" + Util.bytesToHexString(shahash) + ":"
                + unk);
    }
}
}

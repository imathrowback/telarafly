using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class NiSourceTexture : NiTexture
    {
        public String texFilename;
        private bool externalTex;
        private int pixLinkID;
        private int mipMapped;
        private int alphaFormat;
        private bool texStatic;
        private int texIndex;

    public override void parse( NIFFile file,  NIFObject baseo,  BinaryReader ds) 
        {
            base.parse(file, baseo, ds);

            loadObjectNET(file, ds);

            externalTex = ds.readUnsignedByte() > 0;
            texFilename = file.loadString(ds);
            pixLinkID = ds.readInt();
            mipMapped = ds.readInt();
            alphaFormat = ds.readInt();
            texStatic = ds.readUnsignedByte() > 0;

        int unk1 = ds.readUnsignedByte();
        int unk2 = ds.readUnsignedByte();
        texIndex = -1;
		file.addTexture(this);

	}
}
}

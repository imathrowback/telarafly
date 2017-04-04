using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NiDataStream : NIFObject
    {
        public int streamSize;
        public int streamClone;
        public List<StreamRegion> streamRegions;
        public byte[] streamData;
        public bool streamable;
        public int elemStride;


        public override void parse(NIFFile file, NIFObject baseo, BinaryReader dis)
        {
            base.parse(file, baseo, dis);
            streamSize = dis.readInt();
            streamClone = dis.readInt();

            int numRegions = dis.readInt();
            streamRegions = new List<StreamRegion>(numRegions);
            for (int i = 0; i < numRegions; i++)
            {
                streamRegions.Add(new StreamRegion(dis.readInt(), dis.readInt()));
            }
            int numElements = dis.readInt();
            streamElems = new List<NifStreamElement>(numElements);
            elemStride = 0;
            for (int i = 0; i < numElements; i++)
            {
                int elemData = dis.readInt();
                NifStreamElement elem = new NifStreamElement((elemData & 0xFF0000) >> 16, (elemData & 0xFF00) >> 8,
                        elemData & 0xFF, elemStride);
                elemStride += elem.count * elem.size;
                streamElems.Add(elem);
            }

            streamData = dis.ReadBytes(streamSize);
            streamable = dis.ReadByte() > 0;

        }
    }
}

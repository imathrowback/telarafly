using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace Assets.DatParser
{
    public static class BinaryReaderExtensions
    {
        public static Boolean EOF(this BinaryReader r)
        {
            return r.BaseStream.Position == r.BaseStream.Length;
        }


        /** Read 2 bytes */
        public static int readUnsignedShort(this BinaryReader r)
        {
            return r.ReadUInt16();
        }

        public static float readFloat(this BinaryReader r)
        {
            return r.ReadSingle();
        }

        public static uint readUInt(this BinaryReader r)
        {
            return r.ReadUInt32();
        }

        public static int readInt(this BinaryReader r)
        {
            return r.ReadInt32();
        }
        public static byte readUByte(this BinaryReader r)
        {
            return r.ReadByte();
        }
        public static byte readUnsignedByte(this BinaryReader r)
        {
            return r.ReadByte();
        }
        public static byte readByte(this BinaryReader r)
        {
            return r.ReadByte();
        }
        public static short readShort(this BinaryReader r)
        {
            return r.ReadInt16();
        }
        public static void readFully(this BinaryReader r, byte[] data)
        {
            byte[] d = r.ReadBytes(data.Length);
            Array.Copy(d, data, data.Length);
        }
    }
}

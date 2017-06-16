using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace Assets.DatParser
{
    public class MyBinaryReader : BinaryReader
    {
        public MyBinaryReader(Stream stream) : base(stream) { }
        public new int Read7BitEncodedInt()
        {
            return base.Read7BitEncodedInt();
        }
    }

    

    public static class BinaryReaderUtil
    {

        /**
	 * Reads an unsigned integer from {@code in}.
	 */
        public static int readUnsignedLeb128_XX(BinaryReader diss)
        {

        int result = 0;
        int i = 0;
        int index = 0;
        sbyte currentByte;

		while (i< 35)
		{
			index = i;
			i += 7;
			currentByte = diss.ReadSByte();
			result |= (currentByte & 0x7f) << index;
            //Debug.Log("readUnsignedLeb128_XX:[" + i + "]" + result + " cbyte:" + currentByte);
            if (currentByte >= 0)
			{
				return result;
			}
}
		return 0;
	}

        /**
   * Reads an unsigned integer from {@code in}.
   */
        public static int readUnsignedLeb128_X(this BinaryReader br)
        {
            return readUnsignedLeb128_XX(br);
            //MyBinaryReader diss = new MyBinaryReader(br.BaseStream);
            //return diss.Read7BitEncodedInt();
        }
    

   
    public static long readUnsignedVarLong( this BinaryReader din, MemoryStream bos)
    {
        long value = 0L;
        int i = 0;
        long b;
		while (true)
		{
			byte bb = din.ReadByte();
			if (bos != null)
				bos.WriteByte(bb);
			b = bb;
			if ((b & 0x80L) != 0)
			{
				value |= (b & 0x7F) << i;
				i += 7;
				if (i > 63)
				{
					throw new Exception("Variable length quantity is too long");
    }
				continue;
			}
			break;
		}
		return value | (b << i);
	}

    /**
	 * @param in to read bytes from
	 * @return decode value
	 * @throws IOException if {@link DataInput} throws {@link IOException}
	 * @throws IllegalArgumentException if variable-length value does not terminate
	 *             after 9 bytes have been read
	 * @see #writeSignedVarLong(long, DataOutput)
	 */
	public static long readSignedVarLong(this BinaryReader din, MemoryStream bos)
        {
		long raw = readUnsignedVarLong(din, bos);
		// This undoes the trick in writeSignedVarLong()
		long temp = (((raw << 63) >> 63) ^ raw) >> 1;
		// This extra step lets us deal with the largest signed values by treating
		// negative results from read unsigned methods as like unsigned values
		// Must re-flip the top bit if the original read value had it set.
		return temp ^ (raw & (1L << 63));
}
    }
}

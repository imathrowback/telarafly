using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CFileTimeConvertor : CObjectConverter
    {
        private CFileTimeConvertor()
        {
        }

        public override object convert(CObject obj)
        {
            return readFileTime(getDIS(obj));
        }

        public static int NANO100_TO_MILLI = 10000;
        public static long WINDOWS_TO_UNIX_EPOCH = 0x19DB1DED53E8000L;
        internal static readonly CObjectConverter inst = new CFileTimeConvertor();

        public static DateTime readFileTime(BinaryReader diss)
        {

            long lowOrder = diss.readInt();
            long highOrder = diss.readInt();
            long windowsTimeStamp = (highOrder << 32) | lowOrder;
            long milliseconds = ((windowsTimeStamp - WINDOWS_TO_UNIX_EPOCH) / NANO100_TO_MILLI);
            return (new DateTime(1970, 1, 1)).AddMilliseconds(milliseconds);
        }

    }
}

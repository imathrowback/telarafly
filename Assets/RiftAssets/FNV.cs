using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Assets.RiftAssets
{
   
    class FNV
    {
        private static  uint FNV1_32_INIT = 0x811c9dc5;
        private static  uint FNV1_PRIME_32 = 16777619;
        private static  long FNV1_PRIME_64 = 1099511628211L;
        /**
	 * FNV1 32 bit variant.
	 *
	 * @param data - input byte array
	 * @return - hashcode
	 */
        public static uint hash32( byte[] data)
        {
            return hash32(data, data.Length);
        }

        /**
         * FNV1 32 bit variant.
         *
         * @param data - input byte array
         * @param length - length of array
         * @return - hashcode
         */
        public static uint hash32( byte[] data,  int length)
        {
            uint hash = FNV1_32_INIT;
            for (int i = 0; i < length; i++)
            {
                hash *= FNV1_PRIME_32;
                hash ^= (data[i]);
            }

            return hash;
        }
    }
}

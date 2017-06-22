using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.RiftAssets
{
    class Util
    {
        public static string bytesToHexString(byte[] inb)
        {
            return BitConverter.ToString(inb).Replace("-", string.Empty).ToLower();
        }


        public static byte[] hexStringToBytes(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static IEnumerable<string> findIDsForFilename( string name,  Manifest manifest)
        {
            try
            {
                return manifest.filenameHashToID(hashFileName(name));
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to find asset '" + name + "' in manifest", ex);
            }

        }
        public static string hashFileName(string name)
        {
            string lower = name.ToLower();
            byte[] bytes = Encoding.ASCII.GetBytes(lower);
            uint hash = FNV.hash32(bytes);
            string newHash = hash.ToString("X");
            return newHash.PadLeft(8, '0').ToLower();
        }
    }
}
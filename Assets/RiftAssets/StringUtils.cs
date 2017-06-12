using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.RiftAssets
{
    class StringUtils
    {
        public static string leftPad(String str, int count, char c)
        {
            return str.PadLeft(count, c);
        }
    }
}

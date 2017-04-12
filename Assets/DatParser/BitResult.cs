using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class BitResult
    {
        public BitResult( int code,  int memberIndex)
        {
            this.code = code;
            data = memberIndex;
        }

        public int code;
        public int data;

        
        public  override String ToString()
        {
            return "c[" + code + "]d[" + data + "]";
        }
    }
}

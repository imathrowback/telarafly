using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class NifStreamElement
    {
        public int dataType;
        public int offset;
        public NifStreamElement()
        {

        }
        public NifStreamElement( int count,  int size,  int dataType,  int offset)
        {
            this.count = count;
            this.size = size;
            this.dataType = dataType;
            this.offset = offset;
        }

        public int count;
        public int size;
    }
}

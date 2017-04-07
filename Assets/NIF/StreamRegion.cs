using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class StreamRegion
    {
        public StreamRegion()
        {

        }
        public StreamRegion(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
        public int a;
        public int b;
    }
}

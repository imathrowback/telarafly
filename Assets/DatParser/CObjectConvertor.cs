using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    
public abstract class CObjectConverter
    {
        private BinaryReader getLEDIS(byte[] data)
        {
            return new BinaryReader(new MemoryStream(data));
        }
        public BinaryReader getDIS(CObject obj)
        {
            return getLEDIS(obj.data);
        }

        public abstract object convert(CObject obj);

        public override string ToString()
        {
            return this.GetType().Name;
        }


    }

}

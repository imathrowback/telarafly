using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CUnsignedVarLongConvertor : CObjectConverter
    {
        internal static readonly CObjectConverter inst = new CUnsignedVarLongConvertor();

        public override object convert(CObject obj)
        {
            return getDIS(obj).readUnsignedVarLong(null);
        }
    }
}

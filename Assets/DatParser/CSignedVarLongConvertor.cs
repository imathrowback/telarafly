using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CSignedVarLongConvertor : CObjectConverter
    {
        internal static readonly CObjectConverter inst = new CSignedVarLongConvertor();

        public override object convert(CObject obj)
        {
            return getDIS(obj).readSignedVarLong(null);
        }
    }
}

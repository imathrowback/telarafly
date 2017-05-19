using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CDoubleConvertor : CObjectConverter
    {
        internal static readonly CObjectConverter inst = new CDoubleConvertor();

        private CDoubleConvertor()
        {
        }

        public override object convert(CObject obj)
        {
		    return getDIS(obj).ReadDouble();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CDoubleConvertor : CObjectConverter
    {
        public CDoubleConvertor()
        {
        }

        public override object convert(CObject obj)
        {
		    return getDIS(obj).ReadDouble();
        }
    }
}

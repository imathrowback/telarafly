using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CIntConvertor : CObjectConverter
    {
        public static CIntConvertor inst = new CIntConvertor();
        private CIntConvertor()
        {

        }

        public override object convert(CObject obj)
        {
		    return getDIS(obj).ReadInt32();
        }
    }
}

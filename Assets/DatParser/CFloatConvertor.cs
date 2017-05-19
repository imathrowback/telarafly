using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    public class CFloatConvertor : CObjectConverter
    {
        public static CFloatConvertor inst = new CFloatConvertor();

        private CFloatConvertor()
        {

        }


        public override object convert(CObject obj)
        {
		return getDIS(obj).ReadSingle();
        }
    }
}

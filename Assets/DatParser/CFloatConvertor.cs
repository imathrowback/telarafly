using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    public class CFloatConvertor : CObjectConverter
    {

        public CFloatConvertor()
        {

        }


        public override object convert(CObject obj)
        {
		return getDIS(obj).ReadSingle();
        }
    }
}

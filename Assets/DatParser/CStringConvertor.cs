using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    public class CStringConvertor : CObjectConverter
    {

        public CStringConvertor()
        {
        }


        public override object convert(CObject obj)
        {
            return Encoding.ASCII.GetString(obj.data);
        }
    }
}

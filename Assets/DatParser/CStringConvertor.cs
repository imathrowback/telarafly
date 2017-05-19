using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    public class CStringConvertor : CObjectConverter
    {
        internal static readonly CObjectConverter inst = new CStringConvertor();

        private CStringConvertor()
        {
        }


        public override object convert(CObject obj)
        {
            return Encoding.ASCII.GetString(obj.data);
        }
    }
}

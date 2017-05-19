using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CLongConvertor : CObjectConverter
    {
        internal static readonly CObjectConverter inst = new CLongConvertor();

        private CLongConvertor()
        {
        }


        public override object convert(CObject obj)
        {
            if (obj.data.Length == 1)
                return long.Parse("" + getDIS(obj).ReadBoolean());
            else
                return getDIS(obj).ReadInt64();
        }
    }
}

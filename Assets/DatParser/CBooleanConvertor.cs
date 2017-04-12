using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CBooleanConvertor : CObjectConverter
    {
        public CBooleanConvertor()
        {
        }

        public override object convert(CObject obj)
        {
            return getDIS(obj).ReadBoolean();
        }
    }
}

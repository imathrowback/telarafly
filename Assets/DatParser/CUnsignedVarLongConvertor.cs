﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CUnsignedVarLongConvertor : CObjectConverter
    {
        public override object convert(CObject obj)
        {
            return getDIS(obj).readUnsignedVarLong(null);
        }
    }
}
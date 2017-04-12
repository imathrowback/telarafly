using Assets.RiftAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    class CHexConvertor : CObjectConverter
    {

        public override object convert(CObject obj)
        {
		if (obj.data == null)
			return "";

		return Util.bytesToHexString(obj.data);
	}
}
}

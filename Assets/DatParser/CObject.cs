using Assets.DatParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    public class CObject
    {
        public byte[] data;
        public int datacode;
        private CObjectConverter convertor;

        public CObjectConverter getConvertor()
        {
            return convertor;
        }

        public void setConvertor(CObjectConverter convertor)
        {
            this.convertor = convertor;
        }

        public Object convert()
        {
            try
            {
                return getConvertor().convert(this);
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public CObject(int type, byte[] data, int datacode, CObjectConverter convertor)
        {
            this.type = type;
            this.datacode = datacode;
            this.data = data;
            this.convertor = convertor;
            if (data.Length == 0)
                data = null;
        }

        public CObject(int type, MemoryStream data, int datacode, CObjectConverter convertor)
        {
            this.type = type;
            this.datacode = datacode;
            data.Seek(0, SeekOrigin.Begin);
            this.data = data.ToArray();
            this.convertor = convertor;
        }




       public CObject parent;
        public int type;
        public List<CObject> members = new List<CObject>();

        public void addMember( CObject newObj)
        {
            members.Add(newObj);
            newObj.parent = this;
        }

        public override String ToString()
        {
            switch (type)
            {
                case 10:
                case 11:
                    return "array: elements:" + datacode;

            }
            return "obj: " + type;
        }

        public CObject get( int i)
        {
            return members[i];
        }
    }
}

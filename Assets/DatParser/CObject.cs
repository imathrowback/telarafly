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
            index = datacode;
            this.data = data;
            this.convertor = convertor;
            if (data.Length == 0)
                data = null;
        }

        public CObject(int type, MemoryStream data, int datacode, CObjectConverter convertor)
        {
            this.type = type;
            this.datacode = datacode;
            index = datacode;
            data.Seek(0, SeekOrigin.Begin);
            this.data = data.ToArray();
            this.convertor = convertor;
        }
        public Dictionary<int, CObject> asDict()
        {
            if (type != 12)
                throw new Exception("datatype[" + type + "] is not dictionary type 12:" + this);
            Dictionary<int, CObject> dict = new Dictionary<int, CObject>();
            for (int i = 0; i < members.Count; i+=2)
            {
                int a = getIntMember(i);
                CObject b = getMember(i + 1);
                dict[a] = b;

            }
            return dict;
        }
        public int getIntMember(int i)
        {
            CObject member = getMember(i);
            object o = member.convert();
            if (o is int)
                return (int)o;
            if (o is long)
                return (int)((long)o);


            return (int)CIntConvertor.inst.convert(getMember(i));
        }


        public CObject parent;
        public int type;
        public List<CObject> members = new List<CObject>(10);
        // index of this member in it's parent
        internal int index;

        public void addMember(CObject newObj)
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
        public bool hasMember(int index)
        {
            return getMember(index) != null;
        }
        public CObject getMember(int index)
        {
            for (int i = 0; i < members.Count; i++)
                if (members[i].index == index)
                    return members[i];
            return null;
        }

        public CObject get(int i)
        {
            return members[i];
        }

        internal void hintCapacity(int count)
        {
            this.members.Capacity = count;
        }
    }
}

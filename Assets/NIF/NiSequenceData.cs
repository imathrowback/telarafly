using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class NiSequenceData : NIFObject
    {
        public List<uint> seqEvalIDList;
        public string seqName;

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);
            this.seqName = file.loadString(ds);
            var numEval = ds.readUInt();
            this.seqEvalIDList = new List<uint>();
            for (int i = 0; i < numEval; i++)
                this.seqEvalIDList.Add(ds.readUInt());
            file.addSequence(this);
        }
    }
}

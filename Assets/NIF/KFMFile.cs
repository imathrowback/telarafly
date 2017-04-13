using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    public class KFAnimation
    {
        public int id;
        public string sequenceFilename;
        public string sequencename;

        public KFAnimation(int id, string sequenceFilename, string sequencename)
        {
            this.id = id;
            this.sequenceFilename = sequenceFilename;
            if (sequencename.Count() == 0)
            {
                this.sequencename = Path.GetFileNameWithoutExtension(sequenceFilename);
            }
            else
                this.sequencename = sequencename;
        }
    }

    public class KFMFile
    {
        public List<KFAnimation> kfanimations = new List<KFAnimation>();
        public KFMFile(Stream stream)
        {
            // Read header
            using (BinaryReader dis = new BinaryReader(stream))
            {
                string header = readHeaderString(dis);
                if (header.Contains("KFM"))
                {
                    int endian = dis.ReadByte();
                    String rigPath = readString(dis, dis.readInt());    // kModelPath
                    String rootBone = readString(dis, dis.readInt());   // kModelRoot
                    int syncTrans = dis.readInt();
                    int nonSyncTrans = dis.readInt();
                    float syncTransDuraction = dis.readFloat();
                    float nonSyncTransDuration = dis.readFloat();
                    int numSequences = dis.readInt();
                    //Debug.Log("num:" + numSequences);
                    for (int i = 0; i < numSequences; i++)
                    {
                        int id = dis.readInt();
                        int seqfilecount = dis.readInt();
                        string sequenceFilename = readString(dis, seqfilecount);   // kFilename
                        //Debug.Log(seqfilecount + ":" + sequenceFilename);
                        int seqnameCount = dis.readInt();
                        //Debug.Log(seqnameCount);
                        string sequencename = readString(dis,seqnameCount);       // kSequenceName
                        // uh oh
                        float f1 = dis.readFloat();
                        float f2 = dis.readFloat();
                        int i1 = dis.readInt();
                        //int i2 = dis.readInt();
                        //Debug.Log("[" + id + "]:" + sequenceFilename + ":" + sequencename + ":" + f1 + ":" +f2 + ":" + i1 );
                        for (int j = 0; j < i1; j++)
                        {
                            dis.readInt();
                            dis.readInt();
                            dis.readFloat();
                            dis.readInt();
                            dis.readInt();

                        }
                        kfanimations.Add(new KFAnimation(id, sequenceFilename, sequencename));
                    }
                }
            }
        }
        private String readHeaderString(BinaryReader dis)
        {
            String buffer = "";
            while (!dis.EOF())
            {
                char ch = dis.ReadChar();
                if (ch != 0x0A)
                    buffer += ch;
                else
                    break;
            }
            return buffer;
        }
        private String readString(BinaryReader dis, int strLen)
        {
            if (strLen == 0)
                return "";
            return new String(dis.ReadChars(strLen));
        }

    }
}

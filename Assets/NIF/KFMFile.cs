using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    class KFMFile
    {
        public KFMFile(Stream stream)
        {
            // Read header
            using (BinaryReader dis = new BinaryReader(stream))
            {
                string header = readHeaderString(dis);
                if (header.Contains("KFM"))
                {
                    int endian = dis.ReadByte();
                    String rigPath = readString(dis, dis.readInt());
                    String rootBone = readString(dis, dis.readInt());
                    int syncTrans = dis.readInt();
                    int nonSyncTrans = dis.readInt();
                    float syncTransDuraction = dis.readFloat();
                    float nonSyncTransDuration = dis.readFloat();

                    int numSequences = dis.readInt();
                    for (int i = 0; i < numSequences; i++)
                    {
                        int id = dis.readInt();
                        String sequenceFilename = readString(dis, dis.readInt());
                        Debug.Log(sequenceFilename);
                        int animIndex = dis.readInt();
                        int transitions = dis.readInt();
                        Debug.Log("animIndex:" + animIndex + ", transitions:" + transitions);
                        for (int j = 0; j < transitions; j++)
                        {
                            int desID = dis.readInt();
                            int eType = dis.readInt();
                            Debug.Log(eType);
                        }
                    }


                    Debug.Log(rigPath);
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
            return new String(dis.ReadChars(strLen));
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace Assets.NIF
{
    class NIFFile
    {
        uint fileVer;
        bool littleEndian;
        uint userVersion;
        int numObjects;
        String header;
        List<NIFObject> objects;
        List<String> stringTable;
        List<int> groupSizes;
        List<NiMesh> nifMeshes = new List<NiMesh>();
        List<NiSourceTexture> nifTextures = new List<NiSourceTexture>();

        long NIF_INVALID_LINK_ID = 0xFFFFFFFF;

        public NIFFile(Stream stream) 
        {
            parseFile(stream);
        }

        public String getStringFromTable(int i)
        {
            return stringTable[i];
        }

        public List<NIFObject> getObjects()
        {
            return objects;
        }

        public List<String> getStringTable()
        {
            return stringTable;
        }

        private void parseFile(Stream stream)
        {
            // Read header
            using (BinaryReader dis = new BinaryReader(stream))
            {
                readHeader(dis);
                objects = new List<NIFObject>(numObjects);
                for (int i = 0; i < numObjects; i++)
                    objects.Add(new NIFObject());
                loadTypeNames(dis);
                loadObjectSizes(dis);
                loadStringTable(dis);
                loadObjectGroups(dis);
                loadObjects(dis);
            }
        }

        private void loadObjects( BinaryReader dis) 
        {
            for (int i = 0; i<numObjects; i++)
            {
                NIFObject obj = objects[i];
                obj.index = i;
                String typeName = obj.typeName;
                int size = obj.nifSize;
                byte[] data;

                try
                {
                    data = dis.ReadBytes(size);
                    using (BinaryReader ds = new BinaryReader(new MemoryStream(data)))
                    {
                        if (typeName.StartsWith("NiDataStream"))
                        {
                            NiDataStream newObj = new NiDataStream();
                            newObj.parse(this, obj, ds);
                            objects[i] = newObj;
                        }
                        else
                        {
                            NIFObject newObj = (NIFObject)Activator.CreateInstance(null, "Assets.NIF." + typeName).Unwrap();
                            newObj.parse(this, obj, ds);
                            objects[i] = newObj;
                        }
                    }
                } catch (Exception ex)
                {
                    //Debug.Log("Unhandled nif type:" + typeName + " due to exception:" + ex.Message);
                    //Debug.Log("data size:" + obj.nifSize);
                    continue;
                }
            }
                        
            // set parents!
            foreach (NIFObject obj  in objects)
            {
                if (obj is NiNode)
                {
                    NiNode node = (NiNode)obj;
                    foreach (int childID in node.childLinks)
                    {
                        if (childID != NIF_INVALID_LINK_ID)
                        {
                            if (objects[childID].parentIndex != -1)
                            {
                                Debug.LogWarning("WARNING: Node is parented by more than one other node.");
                            }
                            objects[childID].parentIndex = obj.index;
                        }
                    }
                }
            }
        }


        private void loadObjectGroups(BinaryReader dis) 
        {
            int numGroups = dis.readInt();
            groupSizes = new List<int>(numGroups + 1);
            groupSizes.Add(0);
            for (int i = 0; i<numGroups; i++)
		    {
    			groupSizes.Add(dis.readInt());
		    }
        }

        private void loadStringTable( BinaryReader dis) 
        {
            int numStrings = dis.readInt();
            int maxStringSize = dis.readInt();

            stringTable = new List<String>(numStrings);

            for (int i = 0; i<numStrings; i++)
		    {
			    int strLen = dis.readInt();
			    if (strLen > 0)
				    stringTable.Add(readString(dis, strLen));
			    else
				    stringTable.Add("");
		    }

        }

        private void loadObjectSizes( BinaryReader dis) 
        {
		    for (int i = 0; i<numObjects; i++)
			    objects[i].nifSize = dis.readInt();
        }

        private void loadTypeNames(BinaryReader dis) 
        {

            int numTypes = dis.readUnsignedShort();
		    if (numTypes <= 0)
			    throw new Exception("No type entries");
            List<String> types = new List<String>();

		    for (int i = 0; i < numTypes; i++)
		    {
			    int strLen = dis.readInt();
			    if (strLen > 1024)
				    throw new Exception("Too long string entry?");

                types.Add(readString(dis, strLen));
		    }

		    //System.out.println(types);
		    for (int i = 0; i<numObjects; i++)
		    {
			    int typeIndex = dis.readUnsignedShort();
                typeIndex &= ~32768;
			    if (typeIndex > types.Count())
				    throw new Exception("TypeIndex out of bounds");

                objects[i].typeName = types[typeIndex];
		    }

    	}

        private void readHeader(BinaryReader dis)
        {
            header = readHeaderString(dis);
            fileVer = dis.readUInt();
            littleEndian = dis.readUByte() > 0;
            // TODO: handle endianess
            userVersion = dis.readUInt();
            numObjects = dis.readInt();

            if (false)
            Debug.Log("NIF version:" + ((fileVer >> 24) & 255) + "." + ((fileVer >> 16) & 255) + "."
				+ ((fileVer >> 8) & 255) + "." + (fileVer & 255));

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

        public String loadString(BinaryReader ds) 
        {

            int index = ds.readInt();
		    if (index >= 0)
			    return stringTable[index];
		    return "";
	    }

        public void addMesh( NiMesh niMesh)
        {
            nifMeshes.Add(niMesh);

        }

        public void addTexture(NiSourceTexture niSourceTexture)
        {
            nifTextures.Add(niSourceTexture);

        }

        public List<NiMesh> getMeshes()
        {
            return nifMeshes;
        }

        internal NIFObject getObject(int id)
        {
            try
            {
                return objects[id];
            }catch (ArgumentOutOfRangeException ex)
            {
                String s = ("Unable to get object for id[" + id + "], numObjects:" + objects.Count);
                Debug.LogError(s);
                throw new Exception(s, ex);
            }
        }
    }
}

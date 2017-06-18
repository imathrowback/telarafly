using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace Assets.NIF
{
    public class NIFFile
    {
        public uint fileVer;
        public bool littleEndian;
        public uint userVersion;
        public int numObjects;
        public String header;
        public List<NIFObject> objects;
        public List<String> stringTable;
        public List<int> groupSizes;
        public List<NiMesh> nifMeshes = new List<NiMesh>();
        public List<NiSourceTexture> nifTextures = new List<NiSourceTexture>();
        public List<NiSequenceData> nifSequences = new List<NiSequenceData>();
        long NIF_INVALID_LINK_ID = 0xFFFFFFFF;

        internal void addSequence(NiSequenceData niSequenceData)
        {
            nifSequences.Add(niSequenceData);
        }

        public NIFFile()
        {
        }
        public NIFFile(Stream stream, bool skinMesh = false) 
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

        private void parseFile(Stream stream, bool skinMesh = false)
        {
            
            // Read header
            using (BinaryReader dis = new BinaryReader(stream))
            {
                readHeader(dis);
                if (header.Contains("KFM"))
                {
                    String rigPath = readString(dis, dis.readInt());
                    Debug.Log(rigPath);
                }
                else
                {
                    objects = new List<NIFObject>(numObjects);
                    for (int i = 0; i < numObjects; i++)
                        objects.Add(new NIFObject());
                    //Debug.Log("start loadTypeNames pos:" + dis.BaseStream.Position);
                    loadTypeNames(dis);
                    //Debug.Log("start loadObjectSizes pos:" + dis.BaseStream.Position);
                    loadObjectSizes(dis);
                    //Debug.Log("start loadStringTable pos:" + dis.BaseStream.Position);
                    loadStringTable(dis);
                    //Debug.Log("start loadObjectGroups pos:" + dis.BaseStream.Position);
                    loadObjectGroups(dis);
                    //Debug.Log("start loadObjects pos:" + dis.BaseStream.Position);
                    loadObjects(dis);
                }
            }
        }

        private void loadObjects(BinaryReader dis)
        {

            for (int i = 0; i < numObjects; i++)
            {
                NIFObject obj = objects[i];
                obj.index = i;
                String typeName = obj.typeName;
                int size = obj.nifSize;
                byte[] data;
                String cName = "Assets.NIF." + typeName;

                

                try
                {

                    long pos = dis.BaseStream.Position;
                    data = dis.ReadBytes(size);

                    if (notImplementedMap.ContainsKey(typeName))
                        continue;
                    
                    using (BinaryReader ds = new BinaryReader(new MemoryStream(data, false)))
                    {
                        if (typeName.StartsWith("NiDataStream"))
                        {
                            NiDataStream newObj = new NiDataStream();
                            newObj.parse(this, obj, ds);
                            objects[i] = newObj;
                        }
                        else
                        {
                            NIFObject newObj;

                            //if (typeName.Contains("Eval"))
                            //Debug.Log("[" + i + "]: type[" + typeName + "] @ " + pos);
                            if (typeCacheC.ContainsKey(typeName))
                            {
                                newObj = (NIFObject)typeCacheC[typeName].Invoke();
                            }
                            else
                            {
                                Type t = Type.GetType(cName);
                                if (t == null)
                                {
                                    Debug.LogWarning("type " + cName + " not implemented");
                                    notImplementedMap[typeName] = true;
                                    continue;
                                }
                                Debug.LogWarning("[PERFORMANCE WARNING] using activator for " + typeName);
                                newObj = (NIFObject)Activator.CreateInstance(t);
                            }

                            objects[i] = newObj;

                            try
                            {

                                newObj.parse(this, obj, ds);
                            }
                            catch (Exception ex)
                            {
                                Debug.Log(ex);
                            }
                        }
                    }
                    //Debug.Log("[" + i + "]: " + objects[i].GetType());
                } catch (Exception ex)
                {
                    Debug.Log(typeName + ":" + ex);
                    //Debug.Log("Unhandled nif type:" + typeName + " due to exception:" + ex.Message + " :data size:" + obj.nifSize);
                    notImplementedMap[typeName] = true;
                    continue;
                }
            }

            setParents();
        }
        void setParents()
        {              
            // set parents!
            foreach (NIFObject obj  in objects)
            {
                if (obj is NiNode)
                {
                    NiNode node = (NiNode)obj;
                    foreach (int childID in node.childLinks)
                    {
                        if (childID != NIF_INVALID_LINK_ID && childID != -1)
                        {
                            //Debug.Log(childID + ":" + objects.Count);
                            if (objects[childID].parentIndex != -1)
                            {
                                Debug.LogWarning("WARNING: Node is parented by more than one other node.");
                            }
                            objects[childID].parentIndex = obj.index;
                            //Debug.Log("parent[" + node.name + "], set child " + objects[childID].name);
                            obj.addChild(objects[childID]);
                        }
                    }
                }
            }
        }

        static Dictionary<string, bool> notImplementedMap = new Dictionary<string, bool>();
        static Dictionary<String, Func<object>> typeCacheC = initTypeCache();

        static Dictionary<String, Func<object>> initTypeCache()
        {
            Dictionary<String, Func<object>> typeCacheCC = new Dictionary<String, Func<object>>();
            typeCacheCC["NiTerrainNode"] = () => new NiTerrainNode();
            typeCacheCC["NiMesh"] = () => new NiMesh();
            typeCacheCC["NiTexture"] = () => new NiTexture();
            typeCacheCC["NiBinaryExtraData"] = () => new NiBinaryExtraData();
            typeCacheCC["NiFloatExtraData"] = () => new NiFloatExtraData();
            typeCacheCC["NiFloatsExtraData"] = () => new NiFloatsExtraData();
            typeCacheCC["NiIntegerExtraData"] = () => new NiIntegerExtraData();
            typeCacheCC["NiColorExtraData"] = () => new NiColorExtraData();
            typeCacheCC["NiNode"] = () => new NiNode();
            typeCacheCC["NiBSplineCompTransformEvaluator"] = () => new NiBSplineCompTransformEvaluator();
            typeCacheCC["NiSourceTexture"] = () => new NiSourceTexture();
            typeCacheCC["NiStringExtraData"] = () => new NiStringExtraData();
            typeCacheCC["NiTexturingProperty"] = () => new NiTexturingProperty();

            typeCacheCC["NiMaterialProperty"] = () => new NiMaterialProperty();
            typeCacheCC["NiBooleanExtraData"] = () => new NiBooleanExtraData();
            typeCacheCC["NiSequenceData"] = () => new NiSequenceData();
            typeCacheCC["NiBSplineData"] = () => new NiBSplineData();
            typeCacheCC["NiBSplineBasisData"] = () => new NiBSplineBasisData();

            typeCacheCC["NiSkinningMeshModifier"] = () => new NiSkinningMeshModifier();

            return typeCacheCC;
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
                string s = "";
			    if (strLen > 0)
				    s = (readString(dis, strLen));
   		        stringTable.Add(s);
                //Debug.Log("\t" + s);
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
            if (header.Contains("KFM"))
            {
                littleEndian = dis.readUByte() > 0;
                return;
            }
            fileVer = dis.readUInt();
            littleEndian = dis.readUByte() > 0;
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

            int index = (int)ds.readUInt();
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
       

        public List<NIFObject> getChildren(NIFObject obj)
        {
            List<NIFObject> children = new List<NIFObject>();
            foreach (NIFObject ni in objects)
            {
                if (ni.parentIndex == obj.index)
                    children.Add(ni);
            }
            return children;
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

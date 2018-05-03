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
        public Dictionary<int, NIFObject> objects;
        public List<String> stringTable;
        public List<int> groupSizes;
        public List<NiMesh> nifMeshes = new List<NiMesh>();
        public List<NiSourceTexture> nifTextures = new List<NiSourceTexture>();
        public List<NiSequenceData> nifSequences = new List<NiSequenceData>();
        long NIF_INVALID_LINK_ID = 0xFFFFFFFF;

        public class MeshData
        {
            public List<Vector3> verts = new List<Vector3>();
            public int[] tristest;
            public List<Vector2> uvs = new List<Vector2>();
            public List<List<int>> triangles = new List<List<int>>();
            public List<Vector3> inNormals = new List<Vector3>();
            public List<BoneWeight> boneWeights = new List<BoneWeight>();

        }

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
            prepMeshes();
            //prepChildrenLists();
        }


        private void prepMeshes()
        {
            lock (meshDataDict)
            {
                foreach (NiMesh mesh in getMeshes())
                {
                    MeshData md = prepareMesh(this, mesh);
                    meshDataDict[mesh.index] = md;
                }
            }
        }


        //C5.HashDictionary<int, List<NIFObject>> childrenLists = new C5.HashDictionary<int, List<NIFObject>>();
        /*
        void prepChildrenLists()
        {
            foreach(NIFObject obj in getObjects())
            {
                int p = obj.parentIndex;
                List<NIFObject> children;
                if (!childrenLists.Contains(p))
                {
                    children = new List<NIFObject>();
                    childrenLists.Add(p, children);
                }
                else
                    children = childrenLists[p];
                children.Add(obj);
            }
        }
        */
        public void forEachChildNode(int parentIndex, Action<NiNode> action)
        {
            if (objCache == null)
                getObjects();

            for (int i = 0; i < objCache.Count; i++)
            {
                NIFObject obj = objCache[i];
                if (obj.parentIndex == parentIndex && obj is NiNode)
                    action.Invoke((NiNode)obj);
            }
        }

        /*
        public List<NIFObject> getChildren(int parentIndex)
        {
            if (objCache == null)
                getObjects();

            for (int i = 0; i < objCache.Count; i++)
            {
                NIFObject obj = objCache[i];
                if (obj.parentIndex == parentIndex)
                    list.Add(obj);
                return list;
            }
        }
        */

        /*
        public List<NIFObject> getChildren(NIFObject obj)
        {
            return getChildren(obj.index);
        }
        */

        public MeshData getMeshData(NiMesh ni)
        {
            lock (meshDataDict)
            {
                return meshDataDict[ni.index];
            }
        }

        Dictionary<int, MeshData> meshDataDict = new Dictionary<int, MeshData>();

        public String getStringFromTable(int i)
        {
            return stringTable[i];
        }

        
        C5.ArrayList<NIFObject> objCache = null;
        public IList<NIFObject> getObjects()
        {
            if (objCache == null)
            {
                objCache = new C5.ArrayList<NIFObject>(objects.Count);
                objCache.AddAll(objects.Values);
            }
            return objCache;
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
                    objects = new Dictionary<int, NIFObject>();
                    for (int i = 0; i < numObjects; i++)
                        objects.Add(i, new NIFObject());
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
                                // Debug.LogWarning("[PERFORMANCE WARNING] using activator for " + typeName);
                                Type t = Type.GetType(cName);
                                if (t == null)
                                {
                                    notImplementedMap[typeName] = true;
                                    continue;
                                }
                                else
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
            foreach (NIFObject obj  in objects.Values)
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
            Debug.Log("add mesh:" + niMesh.name);
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

        public static MeshData prepareMesh(NIFFile nf, NiMesh mesh)
        {
            bool IS_TERRAIN = (nf.getStringTable().Contains("terrainL1"));

            StreamAndElement indexStreamObj = mesh.getStreamAndElement(nf, "INDEX", -1);
            NifMeshStream indexStreamRef = indexStreamObj.streamRef;
            NiDataStream indexStream = indexStreamObj.dataStream;
            NifStreamElement indexElem = indexStreamObj.elem;

            StreamAndElement nStreamObj = mesh.getStreamAndElement(nf, "NORMAL", -1);
            NifMeshStream nStreamRef = nStreamObj.streamRef;
            NiDataStream nStream = nStreamObj.dataStream;
            NifStreamElement nElem = nStreamObj.elem;

            StreamAndElement uvStreamObj = mesh.getStreamAndElement(nf, "TEXCOORD", -1);
            StreamAndElement uv2StreamObj = mesh.getStreamAndElement(nf, "TEXCOORD", 1);


            StreamAndElement positionStreamObj = mesh.getStreamAndElement(nf, "POSITION", -1);
            NifMeshStream posStreamRef = positionStreamObj.streamRef;
            NiDataStream posStream = positionStreamObj.dataStream;
            NifStreamElement posElem = positionStreamObj.elem;
            DataType indexDataType = typeForNifDataType(indexStreamObj.elem.dataType);
            DataType posDataType = typeForNifDataType(positionStreamObj.elem.dataType);
            if (posDataType != DataType.FLOAT)
                throw new Exception("Unknown position types");

            MeshData meshData = new MeshData();
            List<Vector3> verts = meshData.verts;
            List<Vector2> uvs = meshData.uvs;
            List<List<int>> triangles = meshData.triangles;
            List<Vector3> inNormals = meshData.inNormals;
            List<BoneWeight> boneWeights = meshData.boneWeights;

            for (int i = 0; i < mesh.numSubMeshes; i++)
            {
                List<int> bonePalette = new List<int>();

                int vOffset = verts.Count;
                /** vertices */
                StreamRegion posRegion = posStream.streamRegions[posStreamRef.submeshRegionMap[i]];
                int posOfs = posRegion.a * posStream.elemStride;
                int posEnd = posOfs + posRegion.b * posStream.elemStride;
                byte[] posStreamData = posStream.streamData;
                using (BinaryReader dis = new BinaryReader(new MemoryStream(posStreamData, posOfs, posEnd - posOfs)))
                {
                    // Debug.Log("\tverts:" + posRegion.b);
                    for (int v = 0; v < (posEnd - posOfs) / posStream.elemStride; v++)
                    {
                        float x = dis.readFloat();
                        float y = dis.readFloat();
                        float z = dis.readFloat();
                        verts.Add(new Vector3(x, y, z));
                    }
                    //Debug.Log("pos: left over: " + (dis.BaseStream.Length - dis.BaseStream.Position));
                }


                /** faces */
                StreamRegion idxRegion = indexStream.streamRegions[indexStreamRef.submeshRegionMap[i]];
                int idxOfs = idxRegion.a * indexStream.elemStride;
                DataType idxType = typeForNifDataType(indexStreamObj.elem.dataType);
                int idxEnd = idxOfs + idxRegion.b * indexStream.elemStride;
                byte[] idxStreamData = indexStream.streamData;

                List<int> tris = new List<int>();
                using (BinaryReader dis = new BinaryReader(new MemoryStream(idxStreamData, idxOfs, idxEnd - idxOfs)))
                {
                    for (int idx = 0; idx < (idxEnd - idxOfs) / indexStream.elemStride; idx++)
                    {
                        int v1x = (dis.readUnsignedShort()) + vOffset;
                        tris.Add(v1x);
                    }
                    //Debug.Log("idx left over:" + (dis.BaseStream.Length - dis.BaseStream.Position));
                }
                triangles.Add(tris);
                {
                    /** uvs */
                    if (uvStreamObj != null)
                    {
                        NiDataStream uvStream = uvStreamObj.dataStream;
                        StreamRegion uvRegion = uvStreamObj.dataStream.streamRegions[uvStreamObj.streamRef.submeshRegionMap[i]];
                        DataType uvType = typeForNifDataType(uvStreamObj.elem.dataType);
                        int uvOfs = uvRegion.a * uvStream.elemStride;
                        int uvEnd = uvOfs + uvRegion.b * uvStream.elemStride;
                        byte[] uvStreamData = uvStream.streamData;
                        //Debug.Log("uv datatype:" + uvType + ":" + uvStream.elemStride);
                        using (BinaryReader dis = new BinaryReader(new MemoryStream(uvStreamData, uvOfs, uvEnd - uvOfs)))
                        {
                            for (int uv = 0; uv < (uvEnd - uvOfs) / uvStream.elemStride; uv++)
                            {
                                float u = dis.readFloat();
                                float v = dis.readFloat();
                                uvs.Add(new Vector2(u, v));
                            }
                        }
                    }

                    /** normals */
                    if (nStreamObj != null)
                    {
                        StreamRegion nRegion = nStreamObj.dataStream.streamRegions[nStreamObj.streamRef.submeshRegionMap[i]];
                        DataType nType = typeForNifDataType(nStreamObj.elem.dataType);
                        int nOfs = nRegion.a * nStream.elemStride;
                        int nEnd = nOfs + nRegion.b * nStream.elemStride;
                        byte[] nStreamData = nStream.streamData;
                        using (BinaryReader dis = new BinaryReader(new MemoryStream(nStreamData, nOfs, nEnd - nOfs)))
                        {
                            for (int n = 0; n < (nEnd - nOfs) / nStream.elemStride; n++)
                            {
                                float x = dis.readFloat();
                                float y = dis.readFloat();
                                float z = dis.readFloat();
                                inNormals.Add(new Vector3(x, y, z));
                            }
                        }
                    }

                    /** bone palette */
                    {
                        StreamAndElement bonePalStreamObj = mesh.getStreamAndElement(nf, "BONE_PALETTE", -1);
                        if (bonePalStreamObj != null)
                        {
                            NifMeshStream bonePalStreamRef = bonePalStreamObj.streamRef;
                            NiDataStream bonePalStream = bonePalStreamObj.dataStream;
                            NifStreamElement bonePalElem = bonePalStreamObj.elem;

                            StreamRegion bonePalRegion = bonePalStreamObj.dataStream.streamRegions[bonePalStreamObj.streamRef.submeshRegionMap[i]];
                            DataType bonePalType = typeForNifDataType(bonePalStreamObj.elem.dataType);
                            int bonePalOfs = bonePalRegion.a * bonePalStream.elemStride;
                            int bonePalEnd = bonePalOfs + bonePalRegion.b * bonePalStream.elemStride;
                            byte[] bonePalStreamData = bonePalStream.streamData;
                            using (BinaryReader dis = new BinaryReader(new MemoryStream(bonePalStreamData, bonePalOfs, bonePalEnd - bonePalOfs)))
                            {
                                for (int n = 0; n < (bonePalEnd - bonePalOfs) / bonePalStream.elemStride; n++)
                                {
                                    bonePalette.Add(dis.readUnsignedShort());
                                }
                            }
                        }
                    }
                    {
                        /** blend indicies */
                        StreamAndElement StreamObj = mesh.getStreamAndElement(nf, "BLENDINDICES", -1);
                        if (StreamObj != null)
                        {
                            NifMeshStream StreamRef = StreamObj.streamRef;
                            NiDataStream Stream = StreamObj.dataStream;

                            StreamRegion Region = StreamObj.dataStream.streamRegions[StreamObj.streamRef.submeshRegionMap[i]];
                            DataType type = typeForNifDataType(StreamObj.elem.dataType);
                            int Ofs = Region.a * Stream.elemStride;
                            int End = Ofs + Region.b * Stream.elemStride;
                            byte[] StreamData = Stream.streamData;
                            //Debug.Log("blendi stride:" + Stream.elemStride + ": type:" + type);

                            // each vertex has a blend index
                            using (BinaryReader dis = new BinaryReader(new MemoryStream(StreamData, Ofs, End - Ofs)))
                            {
                                for (int n = 0; n < (End - Ofs) / Stream.elemStride; n++)
                                {
                                    byte idx1 = dis.ReadByte();
                                    byte idx2 = dis.ReadByte();
                                    byte idx3 = dis.ReadByte();
                                    byte idx4 = dis.ReadByte();
                                    BoneWeight weight = new BoneWeight();
                                    weight.boneIndex0 = bonePalette[idx1];
                                    weight.boneIndex1 = bonePalette[idx2];
                                    weight.boneIndex2 = bonePalette[idx3];
                                    weight.boneIndex3 = bonePalette[idx4];

                                    boneWeights.Add(weight);
                                }
                                //Debug.Log("blend: left over: " + (dis.BaseStream.Length - dis.BaseStream.Position));

                            }
                        }
                    }
                    {
                        /** blend weights */
                        StreamAndElement StreamObj = mesh.getStreamAndElement(nf, "BLENDWEIGHT", -1);
                        if (StreamObj != null)
                        {
                            NifMeshStream StreamRef = StreamObj.streamRef;
                            NiDataStream Stream = StreamObj.dataStream;

                            StreamRegion Region = StreamObj.dataStream.streamRegions[StreamObj.streamRef.submeshRegionMap[i]];
                            DataType type = typeForNifDataType(StreamObj.elem.dataType);
                            int Ofs = Region.a * Stream.elemStride;
                            int End = Ofs + Region.b * Stream.elemStride;
                            byte[] StreamData = Stream.streamData;

                            using (BinaryReader dis = new BinaryReader(new MemoryStream(StreamData, Ofs, End - Ofs)))
                            {
                                int total = (End - Ofs) / Stream.elemStride;
                                // each vertex has weights
                                for (int n = 0; n < total; n++)
                                {
                                    BoneWeight w = boneWeights[n + vOffset];
                                    w.weight0 = dis.readFloat();
                                    w.weight1 = dis.readFloat();
                                    w.weight2 = dis.readFloat();
                                    w.weight3 = 0;
                                    boneWeights[n + vOffset] = w;
                                }
                            }
                        }
                    }
                }
            }

            if (IS_TERRAIN && uvs.Count == 0)
            {
                for (int i = 0; i < verts.Count; i++)
                {
                    Vector3 vert = verts[i];
                    float x = vert.x;
                    float z = vert.z;

                    float u = (x / 256.0f);
                    float v = (z / 256.0f);
                    uvs.Add(new Vector2(u, v));

                }
            }

            List<int> trisList = new List<int>();
            for (int i = 0; i < triangles.Count; i++)
            {
                List<int> tris = triangles[i];
                trisList.AddRange(tris);
            }
            meshData.tristest = trisList.ToArray();
            return meshData;

        }

        enum DataType
        {
            BYTE, UBYTE, SHORT, USHORT, INT, UINT, HALFFLOAT, FLOAT, UNSUPPORTED
        }

        static DataType typeForNifDataType(int nifType)
        {
            if (nifType > 56)
                return DataType.UNSUPPORTED;
            int type = (nifType - 1) >> 2;
            if (type == 0 || type == 2)
                return DataType.BYTE;
            else if (type == 1 || type == 3)
                return DataType.UBYTE;
            else if (type == 4 || type == 6)
                return DataType.SHORT;
            else if (type == 5 || type == 7)
                return DataType.USHORT;
            else if (type == 8 || type == 10)
                return DataType.INT;
            else if (type == 9 || type == 11)
                return DataType.UINT;
            else if (type == 12)
                return DataType.HALFFLOAT;
            else if (type == 13)
                return DataType.FLOAT;
            return DataType.UNSUPPORTED;
        }
    }
}

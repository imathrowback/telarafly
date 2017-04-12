using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Assets.NIF;
using System;
using Assets.RiftAssets;
using Ionic.Zlib;
using System.Xml.Serialization;
using System.Linq;

public class NIFLoader
{



    public Manifest manifest;
    public AssetDatabase db;
    String assetsManifest = "L:\\SteamStuff\\Steam2\\steamapps\\common\\rift\\assets64.manifest";
    String assetsManifest32 = "L:\\SteamStuff\\Steam2\\steamapps\\common\\rift\\assets.manifest";
    String assetsDirectory = "L:\\SteamStuff\\Steam2\\steamapps\\common\\rift\\assets\\";


    // Use this for initialization
    public NIFLoader()
    {
    }

    public void loadManifestAndDB()
    {
        Properties p = new Properties("nif2obj.properties");
        assetsDirectory = (p.get("ASSETS_DIR"));
        assetsManifest = (p.get("ASSETS_MANIFEST64"));
        assetsManifest32 = (p.get("ASSETS_MANIFEST"));
        manifest = new Manifest(assetsManifest32);
        db = AssetProcessor.buildDatabase(manifest, assetsDirectory);
    }



    public GameObject loadNIFFromFile(String fname)
    {
        using (FileStream nifStream = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            NIFFile nf = new NIFFile(nifStream);
            return loadNIF(nf, fname);
        }

    }

    public NIFFile getNIF(String fname)
    {
        byte[] nifData = db.extractUsingFilename(fname);
        using (MemoryStream nifStream = new MemoryStream(nifData))
        {
            return new NIFFile(nifStream);
        }
    }

    public GameObject loadNIF(String fname)
    {

        try
        {
            return loadNIF(getNIF(fname), fname);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Exception trying to load fname:" + fname + ":" + ex);
            return new GameObject();
        }
    }

    public GameObject loadNIF(NIFFile nf, string fname)
    {

        /*
        using (FileStream fs = new FileStream("fname.xml", FileMode.Create))
        {
            Type[] extraTypes = new Type[]
            {
                typeof(NiTerrainNode),
                typeof(NiNode),
                typeof(NiAVObject),
                typeof(NiBinaryExtraData),
                typeof(NiDataStream),
                typeof(NiBooleanExtraData),
                typeof(NiFloatExtraData),
                typeof(NiColorExtraData),
                typeof(NiTexturingProperty),
                typeof(NifMeshStream),
                typeof(NifStreamElement),
                typeof(NiIntegerExtraData),
                typeof(NiMaterialProperty),
                typeof(NiMesh),
                typeof(NiObjectNET),
                typeof(NiProperty),
                typeof(NiRenderObject),
                typeof(NiSourceTexture),
                typeof(NiStringExtraData),
                typeof(StreamAndElement),
                typeof(StreamRegion),
            };
            XmlSerializer x = new XmlSerializer(typeof(NIFFile), extraTypes);
            x.Serialize(fs, nf);
        }
        */
        GameObject root = new GameObject();
        root.name = Path.GetFileNameWithoutExtension(fname);
        root.transform.localPosition = Vector3.zero;
        

        List<NIFObject> rootObjects = getChildren(nf, -1);
       
        foreach (NIFObject obj in rootObjects)
        {
            if (obj is NiNode)
            {
             
                NiNode niNode = (NiNode)obj;
                GameObject node = processNodeAndLinkToParent(nf, (NiNode)obj, root);
            }
        }

        processBones(nf, root);
        return root;
    }

    private void processBones(NIFFile nf, GameObject root)
    {
        if (!nf.skinMesh)
            return;
        List<NiSkinningMeshModifier> skinMods = getSkinMods(nf);
        foreach (NiSkinningMeshModifier skinMod in skinMods)
        {
            Transform rootBone = null;
            List<Transform> bones = new List<Transform>();

            NiMesh mes = null;
            foreach (NIFObject o in nf.objects)
            {
                if (o is NiMesh)
                {
                    if (((NiMesh)o).modLinks.Contains(skinMod.index))
                    {
                        mes = (NiMesh)o;
                        break;
                    }
                }
            }
            Transform meshObject = root.transform.FindDeepChild(mes.name);
            SkinnedMeshRenderer meshRenderer = meshObject.GetComponent<SkinnedMeshRenderer>();
            List<Matrix4x4> bindPoses = new List<Matrix4x4>();
            if (skinMod != null)
            {
                rootBone = root.transform.FindDeepChild(nf.getObject(skinMod.rootBoneLinkID).name);

                List<int> boneLinkIds = skinMod.boneLinkIDs;
                for (int boneIdx = 0; boneIdx < boneLinkIds.Count; boneIdx++)
                {
                    int objId = boneLinkIds[boneIdx];
                    NIFObject ni = nf.getObject(objId);
                    Transform t = root.transform.FindDeepChild(ni.name);

                    if (t != null)
                    {
                        //Debug.Log("link ni bone[" + ni.name + "] with game transform " + t.name);
                        bones.Add(t);
                        NITransform nit = skinMod.m_pkSkinToBoneTransforms[boneIdx];
                        Matrix4x4 m = toMat(nit.matrix).transpose;

                        bindPoses.Add(m);
                    }
                }
            }
            meshRenderer.rootBone = rootBone;
            meshRenderer.bones = bones.ToArray();
            meshRenderer.sharedMesh.bindposes = bindPoses.ToArray();
            meshRenderer.sharedMesh.RecalculateBounds();
        }
    }

    public List<NiSkinningMeshModifier> getSkinMods(NIFFile nf)
    {
        List<NiSkinningMeshModifier> mods = new List<NiSkinningMeshModifier>();
        foreach (NIFObject o in nf.objects)
        {
            if (o is NiSkinningMeshModifier)
                mods.Add((NiSkinningMeshModifier)o);
        }
        return mods;
    }

    List<NIFObject> getChildren(NIFFile nf, int parentIndex)
    {
        List<NIFObject> list = new List<NIFObject>();
        foreach (NIFObject obj in nf.getObjects())
            if (obj.parentIndex == parentIndex)
                list.Add(obj);
        return list;
    }

    GameObject processNodeAndLinkToParent(NIFFile nf, NiNode niNode, GameObject parent)
    {
     

        GameObject goM = new GameObject();
        goM.name = niNode.name;

        //GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //s.transform.parent = goM.transform;
        //s.transform.localScale = Vector3.one * 0.01f;

        foreach (NiMesh mesh in nf.getMeshes())
            {
                if (mesh.parentIndex == niNode.index)
                {
                    GameObject meshGo = processMesh(nf, mesh);
                    if (niNode is NiTerrainNode)
                    {
                        //meshGo.GetComponent<MeshRenderer>().material = new Material(Shader.Find("BasicTerrainShader"));
                        //Debug.Log("found a terrain node");
                    }
                    meshGo.transform.parent = goM.transform;
                }
            }

        List<NIFObject> children = getChildren(nf, niNode.index);
        foreach (NIFObject obj in children)
        {
            if (obj is NiNode)
            {
                GameObject go = processNodeAndLinkToParent(nf, (NiNode)obj, goM);
            }
        }


        goM.transform.parent = parent.transform;

        // Terrain NIFs already have their location set in the parent node, we don't need to set it for the mesh.
        if (!(niNode is NiTerrainNode))
        {
            goM.transform.localPosition = new Vector3(niNode.translation.x, niNode.translation.y, niNode.translation.z);
            Matrix4x4 mat = toMat(niNode.matrix);

            Quaternion q = GetRotation(mat);
            goM.transform.localRotation = q;
            
            //Debug.Log("[" + niNode.name + "]: trans:\n" + mat.GetRow(0) + "\n" + mat.GetRow(1) + "\n" + mat.GetRow(2) + "\n" + mat.GetRow(3));
            //goM.transform.localEulerAngles = 
        }
        else
        {

        }
        goM.transform.localScale = new Vector3(niNode.scale, niNode.scale, niNode.scale);
        //goM.transform.localRotation 
        return goM;

    }

    private Matrix4x4 toMat(Matrix4f m4)
    {
        Matrix4x4 mat = new Matrix4x4();
        mat.m00 = m4.m11;
        mat.m01 = m4.m12;
        mat.m02 = m4.m13;
        mat.m03 = m4.m14;

        mat.m10 = m4.m21;
        mat.m11 = m4.m22;
        mat.m12 = m4.m23;
        mat.m13 = m4.m24;

        mat.m20 = m4.m31;
        mat.m21 = m4.m32;
        mat.m22 = m4.m33;
        mat.m23 = m4.m34;

        mat.m30 = m4.m41;
        mat.m31 = m4.m42;
        mat.m32 = m4.m43;
        mat.m33 = m4.m44;
        
        return mat;
    }

    public static Quaternion GetRotation(Matrix4x4 matrix)
    {
        return Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
    }

    

    GameObject processMesh(NIFFile nf, NiMesh mesh)
    {
        bool IS_TERRAIN = (nf.getStringTable().Contains("terrainL1"));

        //Debug.Log("process mesh:" + mesh.name);
        GameObject go = new GameObject();
        go.name = mesh.name;
        if (mesh.name.Length == 0)
            go.name = "mesh";
        go.transform.localPosition = new Vector3(mesh.translation.x, mesh.translation.y, mesh.translation.z);
        Mesh newMesh = new Mesh();
        MeshFilter mf = go.AddComponent<MeshFilter>();

        Renderer r;
        if (!nf.skinMesh)
        {
            r = go.AddComponent<MeshRenderer>();
        }
        else 
        {
            r = go.AddComponent<SkinnedMeshRenderer>();
            ((SkinnedMeshRenderer)r).quality = SkinQuality.Bone2;
            ((SkinnedMeshRenderer)r).sharedMesh = newMesh;
        }
        
        mf.mesh = newMesh;
        if (Assets.GameWorld.useColliders)
        {
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = newMesh;
        }
        newMesh.subMeshCount = mesh.numSubMeshes;
        if (mesh.meshPrimType != 0) // Triangles
        {
            Debug.Log("unknown meshPrimType:" + mesh.meshPrimType);
        }
        else
        {
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


            List<Vector3> verts = new List<Vector3>();
            int vOffset = verts.Count;
            List<Vector2> uvs = new List<Vector2>();
            List<List<int>> triangles = new List<List<int>>();
            List<Vector3> inNormals = new List<Vector3>();
            List<int> bonePalette = new List<int>();
            List<BoneWeight> boneWeights = new List<BoneWeight>();
            for (int i = 0; i < mesh.numSubMeshes; i++)
            {
                //Debug.Log("Process submesh:" + i);


                /** vertices */
                StreamRegion posRegion = posStream.streamRegions[posStreamRef.submeshRegionMap[i]];

                int start = posRegion.a * posStream.elemStride + posElem.offset;
                byte[] posStreamData = posStream.streamData;
                using (BinaryReader dis = new BinaryReader(new MemoryStream(posStreamData)))
                {
                    for (int v = 0; v < posRegion.b; v++)
                    {
                        float x = dis.readFloat();
                        float y = dis.readFloat();
                        float z = dis.readFloat();
                        verts.Add(new Vector3(x, y, z));
                    }
                }


                /** faces */
                StreamRegion idxRegion = indexStream.streamRegions[indexStreamRef.submeshRegionMap[i]];
                int idxOfs = idxRegion.a * indexStream.elemStride;
                int idxEnd = idxOfs + idxRegion.b * indexStream.elemStride;
                byte[] idxStreamData = indexStream.streamData;
                //Debug.Log("index stride:" + indexStream.elemStride);
                //Debug.Log("index stride:" + indexStream.);
                List<int> tris = new List<int>((idxEnd - idxOfs) / 2);
                using (BinaryReader dis = new BinaryReader(new MemoryStream(idxStreamData, idxOfs, idxEnd - idxOfs)))
                {
                    for (int idx = 0; idx < (idxEnd - idxOfs) / 2 / 3; idx++)
                    {
                        int v1x = (dis.readUnsignedShort()) + vOffset;
                        int v1y = (dis.readUnsignedShort()) + vOffset;
                        int v1z = (dis.readUnsignedShort()) + vOffset;

                        tris.Add(v1x);
                        tris.Add(v1y);
                        tris.Add(v1z);
                    }
                }
                triangles.Add(tris);

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
                          //  Debug.Log("bonepal: left over: " + (dis.BaseStream.Length - dis.BaseStream.Position));
                        }
                       // Debug.Log("bone palette:" + string.Join(",", Array.ConvertAll(bonePalette.ToArray(), item => item.ToString())));
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
                        int End = Ofs + Region.b *Stream.elemStride;
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
                           // Debug.Log("blend: left over: " + (dis.BaseStream.Length - dis.BaseStream.Position));

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
                                BoneWeight w = boneWeights[n];
                                w.weight0 = dis.readFloat();
                                w.weight1 = dis.readFloat();
                                w.weight2 = dis.readFloat();
                                w.weight3 = 0;
                                boneWeights[n] = w;
                            }
                            //Debug.Log("blendw: left over: " + (dis.BaseStream.Length - dis.BaseStream.Position));
                        }
                    }
                }
            }

            newMesh.SetVertices(verts);
           // Debug.Log("v[" + verts.Count + ", bones[" + boneWeights.Count);
            if (inNormals.Count > 0)
                newMesh.SetNormals(inNormals);

           

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
            if (uvs.Count > 0)
                newMesh.SetUVs(0, uvs);
            for (int i = 0; i < triangles.Count; i++)
                newMesh.SetTriangles(triangles[i], i);
            
        if (boneWeights.Count > 0 && !IS_TERRAIN)
            newMesh.boneWeights = boneWeights.ToArray();

            // do materials/textures
            Material mat = new Material(Shader.Find("Standard"));
            if (IS_TERRAIN)
                mat = new Material(Resources.Load("terrainmat", typeof(Material)) as Material);

            if (mesh.materialNames.Contains("Ocean_Water_Shader") || mesh.materialNames.Contains("Flow_Water"))
            {
                mat = new Material(Resources.Load("WaterMaterial", typeof(Material)) as Material);
            }



            r.material = mat;

            foreach (int eid in mesh.extraDataIDs)
            {
                NIFObject obj = nf.getObject(eid);
                if (obj is NiBooleanExtraData)
                {
                    NiBooleanExtraData fExtra = (NiBooleanExtraData)obj;
                    switch (fExtra.extraDataString)
                    {
                        case "doAlphaTest":
                            if (fExtra.booleanData)
                            {
                                Material transmat = new Material(Resources.Load("transmat", typeof(Material)) as Material);

                                mat = transmat;
                                r.material = mat;
                                /*
                                mat.SetFloat("_Mode", 2.0f);
                                // The following is needed to force the engine to listen to our request to set the mode
                                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                mat.SetInt("_ZWrite", 0);
                                mat.DisableKeyword("_ALPHATEST_ON");
                                mat.EnableKeyword("_ALPHABLEND_ON");
                                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                                mat.renderQueue = 3000;
                                */
                            }
                            break;
                        default:
                            break;

                    }
                }
            }
            foreach (int eid in mesh.extraDataIDs)
            {
                NIFObject obj = nf.getObject(eid);
                if (obj is NiFloatExtraData)
                {
                    NiFloatExtraData fExtra = (NiFloatExtraData)obj;
                    switch (fExtra.extraDataString)
                    {
                        case "scaleY":
                            mat.mainTextureScale = new Vector2(mat.mainTextureScale.x, fExtra.floatData);
                            break;
                        case "scale":
                            mat.mainTextureScale = new Vector2(fExtra.floatData, mat.mainTextureScale.y);
                            break;
                        default:
                            break;
                    }
                }
            }

            String[] textureNameIds = getTextureIds(nf, mesh);


            List<int> propIDs = mesh.nodePropertyIDs;
            foreach (int propID in propIDs)
            {
                NIFObject obj = nf.getObject(propID);

                if (obj is NiTexturingProperty)
                {
                    NiTexturingProperty propObj = (NiTexturingProperty)obj;
                    foreach (NifTexMap tex in propObj.texList)
                    {
                        if (tex != null)
                            Debug.Log("\t" + tex.sourceTexLinkID);
                    }

                    int i = 0;
                    foreach (NifTexMap tex in propObj.shaderMapList)
                    {
                        String texName = "";
                        if (tex != null)
                        {
                            int sourceTexID = tex.sourceTexLinkID;
                            if (sourceTexID != -1)
                            {
                                NiSourceTexture sourceTex = (NiSourceTexture)nf.getObject(sourceTexID);
                                texName = sourceTex.texFilename;
                                if (IS_TERRAIN)
                                {
                                    string param = "_terrain" + i;
                                    //Debug.Log("set " + param + " to " + texName + " mat:" + mat.name);
                                    mat.SetTexture(param, loadTexture(db, texName));
                                }
                                else
                                {
                                    switch (textureNameIds[i])
                                    {
                                        case "diffuseTexture":
                                        case "diffuseTextureXZ":
                                            mat.SetTexture("_MainTex", loadTexture(db, texName));
                                            break;
                                        case "decalNormalTexture":
                                            mat.SetTexture("_DetailNormalMap", loadTexture(db, texName));
                                            break;
                                        case "glowTexture":
                                            mat.SetTexture("_EmissionMap", loadTexture(db, texName));
                                            break;
                                        case "glossTexture":
                                            mat.SetTexture("_MetallicGlossMap", loadTexture(db, texName));
                                            break;
                                        case "decalTexture":
                                            mat.SetTexture("_DetailAlbedoMap", loadTexture(db, texName));
                                            break;
                                        default:
                                            //Debug.LogWarning("No shader material property for " + textureNameIds[i]);
                                            break;
                                    }
                                }
                            }
                        }
                        i++;
                    }
                }
            }
        }
        return go;
    }


    Dictionary<String, Texture> toriginals = new Dictionary<string, Texture>();

    public Texture getCachedTObject(string fn)
    {
        if (toriginals.ContainsKey(fn))
        {
            return toriginals[fn];
        }
        return null;
    }

    private Texture loadTexture(AssetDatabase db, String name)
    {

        Texture tex = getCachedTObject(name);
        if (tex != null)
            return tex;
        try
        {
            //Debug.Log("load:" + name);
            String testPath = @"d:\rift_stuff\dds\" + name;
            byte[] data;
            if (File.Exists(testPath) && false)
            {
                data = File.ReadAllBytes(testPath);
            }
            else
            {
                if (db == null)
                {
                    //Debug.Log("db was null");
                    return new Texture2D(2, 2);
                }
                data = db.extractUsingFilename(name);
                //File.WriteAllBytes(testPath, data);
            }
            tex = DDSLoader.DatabaseLoaderTexture_DDS.LoadDDS(data);

        }
        catch (Exception ex)
        {
            Debug.LogWarning("Unable to load texture:" + name + ":" + ex);
            tex = new Texture2D(2, 2);
        }
        tex.name = name;
        toriginals[name] = tex;
        return tex;
    }

    public static Texture2D SimpleLoadTextureDXT(byte[] ddsBytes, TextureFormat textureFormat)
    {
        if (textureFormat != TextureFormat.DXT1 && textureFormat != TextureFormat.DXT5)
            throw new Exception("Invalid TextureFormat. Only DXT1 and DXT5 formats are supported by this method.");

        byte ddsSizeCheck = ddsBytes[4];
        if (ddsSizeCheck != 124)
            throw new Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

        int height = ddsBytes[13] * 256 + ddsBytes[12];
        int width = ddsBytes[17] * 256 + ddsBytes[16];

        int DDS_HEADER_SIZE = 128;
        byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
        Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

        Texture2D texture = new Texture2D(width, height, textureFormat, false);
        texture.LoadRawTextureData(dxtBytes);
        texture.Apply();

        return (texture);
    }

    private String[] getTextureIds(NIFFile nf, NiMesh mesh)
    {
        String[] textureType;
        NiTexturingProperty texturingProperty = mesh.getTexturingProperty(nf);
        if (texturingProperty != null)
        {
            //Debug.Log("found texturing property for mesh " + mesh.name);
            textureType = new String[texturingProperty.shaderMapList.Count];
            foreach (int extraID in mesh.extraDataIDs)
            {
                NIFObject ni = nf.getObject(extraID);
                if (ni is NiIntegerExtraData)
                {
                    NiIntegerExtraData nied = (NiIntegerExtraData)ni;
                    if (nied.extraDataString != null)
                    {
                        if (nied.extraDataString.Contains("Texture"))
                        {
                            if (nied.intExtraData >= 0 && nied.intExtraData < textureType.Length)
                                textureType[nied.intExtraData] = nied.extraDataString;
                            else
                                Debug.LogWarning("nied.intExtraData out of range:" + nied.intExtraData + " => " + textureType.Length);
                        }
                    }
                }
            }
        }
        else
            textureType = new String[255];
        return textureType;
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

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
using Assets;

public class NIFLoader
{

    private NIFLoader()
    {
    }


    static private void prepTextures(NIFFile file)
    {
        for (int i = 0; i < file.getObjects().Count; i++)
        {
            NIFObject obj = file.getObjects()[i];
            if (obj is NiSourceTexture)
            {
                NiSourceTexture tex = (NiSourceTexture)obj;
                try
                {
                    // preload texture
                    AssetDatabaseInst.DB.extractUsingFilename(tex.texFilename, Assets.RiftAssets.AssetDatabase.RequestCategory.TEXTURE);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }


    static public GameObject loadNIFFromFile(String fname, bool skinMesh = false)
    {
        using (FileStream nifStream = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            NIFFile nf = new NIFFile(nifStream);
            prepTextures(nf);
            return loadNIF(nf, fname, skinMesh);
        }

    }

    static Dictionary<string, NIFFile> nifCache = new Dictionary<string, NIFFile>();

    public static NIFFile getNIF(String fname, AssetDatabase.RequestCategory requestCategory = AssetDatabase.RequestCategory.NONE)
    {
        lock (nifCache)
        {
            string key = fname + ":" + requestCategory.ToString();
            NIFFile niffile;

            if (!nifCache.TryGetValue(key, out niffile))
            {
                byte[] nifData = AssetDatabaseInst.DB.extractUsingFilename(fname, requestCategory);
                using (MemoryStream nifStream = new MemoryStream(nifData))
                {
                    niffile = new NIFFile(nifStream);
                    prepTextures(niffile);

                    nifCache[key] = niffile;
                }
            }
            return niffile;
        }
    }

    public static GameObject loadNIF(String fname, bool skinMesh = false)
    {

        try
        {
            return loadNIF(getNIF(fname), fname, skinMesh);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Exception trying to load fname:" + fname + ":" + ex);
            return new GameObject();
        }
    }

    public static GameObject loadNIF(NIFFile nf, string fname,  bool skinMesh = false)
    {
        GameObject root = new GameObject();
        root.name = Path.GetFileNameWithoutExtension(fname);
        root.transform.localPosition = Vector3.zero;

        List<NIFObject> rootObjects = getChildren(nf, -1);
       
        foreach (NIFObject obj in rootObjects)
        {
            if (obj is NiNode)
            {
                NiNode niNode = (NiNode)obj;
                GameObject node = processNodeAndLinkToParent(nf, (NiNode)obj, root, skinMesh);
            }
        }

        if (skinMesh)
            linkBonesToMesh(nf, root);
        return root;
    }

    static public void linkBonesToMesh(NIFFile nf, GameObject skeletonRoot)
    {
        //Debug.Log("link bones to mesh[" + skeletonRoot.GetInstanceID());
        List<NiSkinningMeshModifier> skinMods = getSkinMods(nf);
        foreach (NiSkinningMeshModifier skinMod in skinMods)
        {
            if (skinMod != null)
            {
                List<Transform> bones = new List<Transform>();
                List<Matrix4x4> bindPoses = new List<Matrix4x4>();

                NIFObject rootBoneNode = nf.getObject(skinMod.rootBoneLinkID);
                Transform rootBone = skeletonRoot.transform.FindDeepChild(rootBoneNode.name);

                List<int> boneLinkIds = skinMod.boneLinkIDs;
                for (int boneIdx = 0; boneIdx < boneLinkIds.Count; boneIdx++)
                {
                    int objId = boneLinkIds[boneIdx];
                    NIFObject ni = nf.getObject(objId);
                    Transform t = skeletonRoot.transform.FindDeepChild(ni.name);

                    if (t != null)
                    {
                        bones.Add(t);
                        NITransform nit = skinMod.m_pkSkinToBoneTransforms[boneIdx];
                        Matrix4x4 m = toMat(nit.matrix).transpose;
                        bindPoses.Add(m);
                    }
                }

                
                NiMesh mes = getMeshForMod(nf, skinMod);
                Transform meshObject = skeletonRoot.transform.FindDeepChild(mes.name);
                SkinnedMeshRenderer meshRenderer = meshObject.GetComponent<SkinnedMeshRenderer>();
                //Debug.Log("found mesh renderer: " + meshRenderer.GetInstanceID() + " with bones:" + bones.Count);
                meshRenderer.rootBone = rootBone;
                meshRenderer.bones = bones.ToArray();
                meshRenderer.sharedMesh.bindposes = bindPoses.ToArray();
                meshRenderer.sharedMesh.RecalculateBounds();
            }
        }
    }

    static private NiMesh getMeshForMod(NIFFile nf, NiSkinningMeshModifier skinMod)
    {
        foreach (NIFObject o in nf.getObjects())
        {
            if (o is NiMesh)
            {
                if (((NiMesh)o).modLinks.Contains(skinMod.index))
                {
                    return (NiMesh)o;
                }
            }
        }

        return null;
    }

    static public List<NiSkinningMeshModifier> getSkinMods(NIFFile nf)
    {
        List<NiSkinningMeshModifier> mods = new List<NiSkinningMeshModifier>();
        foreach (NIFObject o in nf.getObjects())
        {
            if (o is NiSkinningMeshModifier)
                mods.Add((NiSkinningMeshModifier)o);
        }
        return mods;
    }

    static List<NIFObject> getChildren(NIFFile nf, int parentIndex)
    {
        List<NIFObject> list = new List<NIFObject>();
        foreach (NIFObject obj in nf.getObjects())
            if (obj.parentIndex == parentIndex)
                list.Add(obj);
        return list;
    }

    static GameObject processNodeAndLinkToParent(NIFFile nf, NiNode niNode, GameObject parent, bool skinMesh)
    {
        GameObject goM = new GameObject();
        goM.name = niNode.name;

        foreach (NiMesh mesh in nf.getMeshes())
            {
                if (mesh.parentIndex == niNode.index)
                {
                    GameObject meshGo = processMesh(nf, mesh, nf.getMeshData(mesh), skinMesh);
                    if (niNode is NiTerrainNode)
                    {
                        //meshGo.GetComponent<MeshRenderer>().material = new Material(Shader.Find("BasicTerrainShader"));
                        //Debug.Log("found a terrain node");
                    }
                    meshGo.transform.parent = goM.transform;
                    meshGo.transform.localScale = new Vector3(mesh.scale, mesh.scale, mesh.scale);
                    Quaternion q = GetRotation(toMat(mesh.matrix));
                    meshGo.transform.localRotation = q;
                //meshGo.transform.localEulerAngles = new Vector3(mesh.ma)
            }
            }

        List<NIFObject> children = getChildren(nf, niNode.index);
        foreach (NIFObject obj in children)
        {
            if (obj is NiNode)
            {
                GameObject go = processNodeAndLinkToParent(nf, (NiNode)obj, goM, skinMesh);
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

    static private Matrix4x4 toMat(Matrix4f m4)
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



  
    /// <summary>
    /// This method needs to be called within an Update method from unity. As such, it should be pretty quick
    /// </summary>
    /// <param name="nf"></param>
    /// <param name="mesh"></param>
    /// <param name="meshData"></param>
    /// <param name="skinMesh"></param>
    /// <returns></returns>
    static GameObject processMesh(NIFFile nf, NiMesh mesh, NIFFile.MeshData meshData, bool skinMesh)
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
        if (!skinMesh)
        {
            r = go.AddComponent<MeshRenderer>();
        }
        else 
        {
            r = go.AddComponent<SkinnedMeshRenderer>();
            // needed to force Unity to use 2 bones. RIFT exposes 3 bones, and if we let Unity choose, it'll try to use 4
            // which will make models look wrong
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
           
            newMesh.SetVertices(meshData.verts);
            if (meshData.inNormals.Count > 0)
               newMesh.SetNormals(meshData.inNormals);
            if (meshData.uvs.Count > 0)
                newMesh.SetUVs(0, meshData.uvs);
            if (meshData.boneWeights.Count > 0 && !IS_TERRAIN && skinMesh)
                newMesh.boneWeights = meshData.boneWeights.ToArray();
            newMesh.triangles = meshData.tristest.ToArray();

            // do materials/textures
            Material mat = new Material(Shader.Find("Standard"));
            mat.enableInstancing = true;
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            if (IS_TERRAIN)
                mat = new Material(Resources.Load("terrainmat", typeof(Material)) as Material);

            if (mesh.materialNames.Contains("Ocean_Water_Shader") || mesh.materialNames.Contains("Flow_Water"))
                mat = new Material(Resources.Load("WaterMaterial", typeof(Material)) as Material);
            //foreach(var matN in mesh.materialNames)
                //if (matN.Contains("water_"))
                if (mesh.name.Contains("water_UP") || mesh.name.Contains("water_DOWN"))
                    mat = new Material(Resources.Load("WaterMaterial", typeof(Material)) as Material);

            if (mesh.materialNames.Contains("TwoSided_Alpha_Specular"))
                mat = new Material(Resources.Load("2sidedtransmat_fade", typeof(Material)) as Material);

            // handle some simple animated "scrolling" textures
            if (mesh.materialNames.Contains("Additive_UVScroll_Distort") || 
                mesh.materialNames.Contains("Alpha_UVScroll_Overlay_Foggy_Waterfall") || mesh.materialNames.Contains("Fat_spike12_m") || mesh.materialNames.Contains("pPlane1_m"))
                {
                    mat = new Material(Resources.Load("2sidedtransmat_fade", typeof(Material)) as Material);

                UVScroll scroller = go.AddComponent<UVScroll>();
                scroller.material = mat;

               NiFloatsExtraData extra = getFloatsExtraData(nf, mesh, "tex0ScrollRate");
                if (extra != null)
                {
                    scroller.xRate = extra.floatData[0];
                    scroller.yRate = extra.floatData[1];
                }

            }


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
                                mat = new Material(Resources.Load("2sidedtransmat", typeof(Material)) as Material);
                            break;
                        default:
                            break;

                    }
                }
            }
            r.material = mat;

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
                                    mat.SetTexture(param, loadTexture(texName));
                                }
                                else
                                {
                                    switch (textureNameIds[i])
                                    {
                                        case "diffuseTexture":
                                        case "diffuseTextureXZ":
                                            mat.SetTexture("_MainTex", loadTexture( texName));
                                            break;
                                        case "decalNormalTexture":
                                            mat.SetTexture("_DetailNormalMap", loadTexture( texName));
                                            break;
                                        case "normalTexture":
                                            mat.SetTexture("_BumpMap", loadTexture( texName));
                                            break;
                                        case "glowTexture":
                                            mat.EnableKeyword("_EMISSION");
                                            
                                            mat.SetColor("_EmissionColor", Color.white*0.5f);
                                            mat.SetTexture("_EmissionMap", loadTexture( texName));
                                            break;
                                        case "glossTexture":
                                            mat.SetTexture("_MetallicGlossMap", loadTexture( texName));
                                            break;
                                        case "decalTexture":
                                            mat.SetTexture("_DetailAlbedoMap", loadTexture(texName));
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

   static private NiFloatsExtraData getFloatsExtraData(NIFFile nf, NiMesh mesh, string v)
    {
        foreach (int eid in mesh.extraDataIDs)
        {
            NIFObject obj = nf.getObject(eid);
            if (obj is NiFloatsExtraData)
            {
                NiFloatsExtraData fExtra = (NiFloatsExtraData)obj;
                if (fExtra.extraDataString.Equals(v))
                    return fExtra;
            }
        }
        return null;
    }

    static Dictionary<String, Texture> toriginals = new Dictionary<string, Texture>();

    static public Texture getCachedTObject(string fn)
    {
        if (toriginals.ContainsKey(fn))
        {
            return toriginals[fn];
        }
        return null;
    }

    static private Texture loadTexture(String name)
    {

        Texture tex = getCachedTObject(name);
        if (tex != null)
            return tex;
        try
        {
            //Debug.Log("load:" + name);
            String testPath = @"d:\rift_stuff\dds\" + name;
            byte[] data;
            if (File.Exists(testPath))
            {
                data = File.ReadAllBytes(testPath);
            }
            else
            {
                AssetDatabase db = AssetDatabaseInst.DB;
                if (db == null)
                {
                    //Debug.Log("db was null");
                    return new Texture2D(2, 2);
                }
                data = db.extractUsingFilename(name, AssetDatabase.RequestCategory.TEXTURE);
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

   static private String[] getTextureIds(NIFFile nf, NiMesh mesh)
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
                            { 
  //                                Debug.LogWarning("nied.intExtraData out of range:" + nied.intExtraData + " => " + textureType.Length);
                            }
                        }
                    }
                }
            }
        }
        else
            textureType = new String[255];
        return textureType;
    }

   
}

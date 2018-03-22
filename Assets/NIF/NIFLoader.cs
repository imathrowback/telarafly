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
using DDSLoader;
using System.Text;

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
                    bool doLoad = false;
                    lock (NIFLoader.texDataCache)
                    {
                        if (!NIFLoader.texDataCache.ContainsKey(tex.texFilename))
                            doLoad = true;
                    }
                    if (doLoad)
                    {
                        byte[] data = AssetDatabaseInst.DB.extractUsingFilename(tex.texFilename, Assets.RiftAssets.AssetDatabase.RequestCategory.TEXTURE);
                        TextureData texData;
                        DDSLoader.DatabaseLoaderTexture_DDS.LoadDDS(data, out texData);
                        lock (NIFLoader.texDataCache)
                        {
                            if (!NIFLoader.texDataCache.ContainsKey(tex.texFilename))
                                NIFLoader.texDataCache.Add(tex.texFilename, texData);
                        }
                    }
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
        if (File.Exists(fname))
        {
            using (FileStream nifStream = new FileStream(fname, FileMode.Open))
            {
                NIFFile niffile = new NIFFile(nifStream);
                prepTextures(niffile);
                return niffile;
            }
        }

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

    public static GameObject loadNIF(NIFFile nf, string fname,  bool skinMesh = false, GameObject prefab = null)
    {
        GameObject root;
        if (prefab != null)
            root = prefab;
        else
            root = new GameObject();
        root.name = Path.GetFileNameWithoutExtension(fname);
        root.transform.localPosition = Vector3.zero;

        nf.forEachChildNode(-1, (obj) => processNodeAndLinkToParent(nf, obj, root, skinMesh));

        if (skinMesh)
            linkBonesToMesh(nf, root);
        return root;
    }

    static public void linkBonesToMesh(NIFFile nf, GameObject skeletonRoot)
    {
       // Debug.Log("link bones to mesh[" + skeletonRoot.GetInstanceID() + "]");
        List<NiSkinningMeshModifier> skinMods = getSkinMods(nf);
        foreach (NiSkinningMeshModifier skinMod in skinMods)
        {
            if (skinMod != null)
            {
                List<Transform> bones = new List<Transform>();
                List<Matrix4x4> bindPoses = new List<Matrix4x4>();

                NIFObject rootBoneNode = nf.getObject(skinMod.rootBoneLinkID);
               // Debug.Log("looking for root bone:" + rootBoneNode.name + " in skeleton root");
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
                meshRenderer.updateWhenOffscreen = true;
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

        nf.forEachChildNode(niNode.index, (obj) => processNodeAndLinkToParent(nf, obj, goM, skinMesh));

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
            bool IS_TERRAIN = (nf.getStringTable().Contains("terrainL1"));

            newMesh.SetVertices(meshData.verts);
            if (meshData.inNormals.Count > 0)
                newMesh.SetNormals(meshData.inNormals);
            if (meshData.uvs.Count > 0)
                newMesh.SetUVs(0, meshData.uvs);
            if (meshData.boneWeights.Count > 0 && !IS_TERRAIN && skinMesh)
                newMesh.boneWeights = meshData.boneWeights.ToArray();
            // huge memory GC issue here....
            newMesh.triangles = meshData.tristest;
            r.material = doMaterials(nf, mesh, go);
        }
        return go;
    }

     static Shader standardShader = null;
    static Material standardMaterial = null;
    static Material doMaterials(NIFFile nf, NiMesh mesh, GameObject go)
    {
        StringBuilder strB = new StringBuilder(20);
        if (standardShader == null)
            standardShader = Shader.Find("Standard");

        bool IS_TERRAIN = (nf.getStringTable().Contains("terrainL1"));
        bool animated = false;
        bool presetMaterial = false;
        string materialName = null;
        Material mat = null;


        Material mat2 = null;
        if (mesh.materialNames.Count > 0)
        {
            strB.Length = 0;
            strB.Append("materials/");
            strB.Append(mesh.materialNames[0]);
            mat2 = Resources.Load<Material>(strB.ToString());
            if (mat2 != null)
                mat = Material.Instantiate<Material>(mat2);
        }
        else
            Debug.LogWarning("No mesh materials found in mesh :" + mesh.name);

        if (mat == null)
        {

            // do materials/textures

            if (IS_TERRAIN)
                materialName = "terrainmat";

            if (mesh.materialNames.Contains("Ocean_Water_Shader") || mesh.materialNames.Contains("Flow_Water") || mesh.name.Contains("water_UP") || mesh.name.Contains("water_DOWN"))
                materialName = "WaterMaterial";

            bool alpha = (mesh.materialNames.Contains("TwoSided_Alpha_Specular") || mesh.materialNames.Contains("Lava_Flow_Decal"));
            foreach (string n in mesh.materialNames)
                if (n.ToLower().Contains("alpha"))
                    alpha = true;
            if (alpha)
                materialName = "2sidedtransmat_fade";

            // handle some simple animated "scrolling" textures
            animated = (mesh.materialNames.Contains("Additive_UVScroll_Distort") || mesh.materialNames.Contains("Lava_Flow_Decal") || mesh.materialNames.Contains("Local_Cloud_Flat") ||
               mesh.materialNames.Contains("Alpha_UVScroll_Overlay_Foggy_Waterfall") || mesh.materialNames.Contains("Fat_spike12_m") || mesh.materialNames.Contains("pPlane1_m"));


            if (animated)
                materialName = "2sidedtransmat_fade";



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
                                materialName = "2sidedtransmat";
                            break;
                        default:
                            break;

                    }
                }
            }
            if (materialName == null)
            {
                if (standardMaterial == null)
                    standardMaterial = new Material(standardShader);
                mat = Material.Instantiate(standardMaterial);
                materialName = standardMaterial.name;
            }
            else
                mat = Material.Instantiate(Resources.Load<Material>(materialName));
            //Debug.Log("Using guessed material[" + materialName + "] for " + mesh.name + " from list of materials: " + string.Join(",", mesh.materialNames.ToArray()), go);
        }
        else if (mat2 != null)
        {
            materialName = mat2.name;
            presetMaterial = true;
            //Debug.Log("Using actual material[" + materialName + "] for " + mesh.name + " from list of materials: " + string.Join(",", mesh.materialNames.ToArray()), go);
        }
        else Debug.LogWarning("No material found!?");
#if UNITY_EDITOR
        MeshOriginalMaterial mom = go.AddComponent<MeshOriginalMaterial>();
        mom.materialName = mesh.materialNames[0];
#endif

        if (presetMaterial)
        {
            foreach (int extraId in mesh.extraDataIDs)
            {
                NIFObject obj = nf.getObject(extraId);
                setMaterialProperty(mat, obj);
            }

            if (mat.HasProperty("doAlphaTest"))
            {
                if (mat.GetInt("doAlphaTest") == 0)
                {
                    strB.Length = 0;
                    strB.Append("materials/");
                    strB.Append(mat2.name);
                    strB.Append("_shader_opaque");

                    string shaderName = strB.ToString();
                    //Debug.Log("loading opaque shader:" + shaderName, go);
                    Shader shader = Resources.Load<Shader>(shaderName);
                    if (shader != null)
                        mat.shader = shader;
                }
            }
           

        }

        mat.enableInstancing = true;
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");



        


        if (animated)
        {
            NiFloatsExtraData extra = getFloatsExtraData(nf, mesh, "tex0ScrollRate");
            if (extra != null)
            {
                UVScroll scroller = go.AddComponent<UVScroll>();
                scroller.material = mat;

                scroller.xRate = extra.floatData[0];
                scroller.yRate = extra.floatData[1];
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
                        if (mat.HasProperty("_MainTex"))
                        {
                            mat.mainTextureScale = new Vector2(mat.mainTextureScale.x, fExtra.floatData);
                        }
                        else
                        {
                            if (mat.HasProperty("scaleY"))
                            {
                                mat.SetFloat("scaleY", fExtra.floatData);
                            }
                            else
                                Debug.LogWarning("While trying to set scaleY, material[" + mat.name + "][" + materialName + "] doesn't have an appropriate texture property");
                        }
                        break;
                    case "scale":
                        if (mat.HasProperty("_MainTex"))
                        {
                            mat.mainTextureScale = new Vector2(fExtra.floatData, mat.mainTextureScale.y);
                        }
                        else
                        {
                            if (mat.HasProperty("scale"))
                            {
                                mat.SetFloat("scale", fExtra.floatData);
                            }
                            else
                                Debug.LogWarning("While trying to set scale, material[" + mat.name + "][" + materialName + "] doesn't have an appropriate texture property");
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        string[] textureNameIds = getTextureIds(nf, mesh);

        if (presetMaterial)
        {
            foreach (int extraId in mesh.extraDataIDs)
            {
                NIFObject obj = nf.getObject(extraId);
                setMaterialProperty(mat, obj);
            }
        }

        if (mat.HasProperty("alphaTestRef"))
            mat.SetFloat("alphaTestRef", 1.0f - mat.GetFloat("alphaTestRef"));

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
                    string texName = "";
                    if (tex != null)
                    {
                        int sourceTexID = tex.sourceTexLinkID;
                        if (sourceTexID != -1)
                        {
                            NiSourceTexture sourceTex = (NiSourceTexture)nf.getObject(sourceTexID);
                            texName = sourceTex.texFilename;
                            if (presetMaterial)
                            {
                                strB.Length = 0;
                                if (IS_TERRAIN)
                                {
                                    strB.Append("_terrain");
                                    strB.Append(i);
                                }
                                else
                                {
                                    strB.Append("_");
                                    strB.Append(textureNameIds[i]);
                                }
                                
                                // Debug.Log("attempt to set texture property :" + propertyName + " with texure:" + texName);

                                enqueSetTexture(mat, strB.ToString(), nf, texName);
                                //mat.SetTexture(propertyName, loadTexture(nf, texName));
                            }
                            else if (IS_TERRAIN)
                            {
                                strB.Length = 0;
                                strB.Append("_terrain");
                                strB.Append(i);
                                string param = strB.ToString();
                                    //"_terrain" + i;
                                //Debug.Log("set " + param + " to " + texName + " mat:" + mat.name);
                                enqueSetTexture(mat, param, nf, texName);
                                //mat.SetTexture(param, loadTexture(nf, texName));
                            }
                            else
                            {
                                //Debug.Log("texName[" + texName + "]: id:" + textureNameIds[i]);
                                try
                                {
                                    switch (textureNameIds[i])
                                    {
                                        case "skyGradientTexture0":
                                        case "diffuseTexture":
                                        case "diffuseTextureXZ":
                                            enqueSetTexture(mat, "_MainTex", nf, texName);
                                            //mat.SetTexture("_MainTex", loadTexture(nf, texName));
                                            break;
                                        case "decalNormalTexture":
                                            enqueSetTexture(mat, "_DetailNormalMap", nf, texName);
                                            //mat.SetTexture("_DetailNormalMap", loadTexture(nf, texName));
                                            break;
                                        case "normalTexture":
                                            enqueSetTexture(mat, "_BumpMap", nf, texName);
                                            //mat.SetTexture("_BumpMap", loadTexture(nf, texName));
                                            break;
                                        case "glowTexture":
                                            mat.EnableKeyword("_EMISSION");
                                            if (mesh.materialNames.Contains("Lava_Flow_Decal"))
                                                mat.SetColor("_EmissionColor", Color.red);
                                            else
                                                mat.SetColor("_EmissionColor", Color.white * 0.5f);
                                            enqueSetTexture(mat, "_EmissionMap", nf, texName);
                                            //mat.SetTexture("_EmissionMap", loadTexture(nf, texName));
                                            break;
                                        case "glossTexture":
                                            enqueSetTexture(mat, "_MetallicGlossMap", nf, texName);
                                            //mat.SetTexture("_MetallicGlossMap", loadTexture(nf, texName));
                                            break;
                                        case "decalTexture":
                                        case "starMapTexture0":
                                            enqueSetTexture(mat, "_DetailAlbedoMap", nf, texName);
                                            //mat.SetTexture("_DetailAlbedoMap", loadTexture(nf, texName));
                                            break;
                                        default:
                                            //Debug.LogWarning("No shader material property for " + textureNameIds[i]);
                                            break;
                                    }
                                }catch (ArgumentOutOfRangeException ex)
                                {
                                    Debug.LogWarning("Texture id[" + i + "] was out of range of the texture name ids: " + textureNameIds.ToList());
                                    //mat.SetTexture("_MainTex", loadTexture(nf, texName));
                                    enqueSetTexture(mat, "_MainTex", nf, texName);
                                    
                                }
                            }
                        }
                    }
                    i++;
                }
            }
        }
        return mat;
    }

    private static void enqueSetTexture(Material mat, string propertyName, NIFFile nf, string texName)
    {
        NIFTexturePool.inst.addQueuedTextureAction(() =>
        {
            mat.SetTexture(propertyName, loadTexture1(nf, texName));
        });


    }

    private static void setMaterialProperty(Material mat, NIFObject obj)
    {
        string name = obj.extraDataString;
        if (!mat.HasProperty(name))
        {
           // Debug.Log("no property " + name + " in material " + mat.name + " using obj:" + obj.GetType());
            return;
        }
        //Debug.Log("try set property " + name + " in material " + mat.name + " using obj:" + obj.GetType());
        if (obj is NiFloatExtraData)
        {
            mat.SetFloat(name, (obj as NiFloatExtraData).floatData);
            //Debug.Log("setting [" + name + "] of material to " + (obj as NiFloatExtraData).floatData);
        }
        else if (obj is NiFloatsExtraData)
        {
            float[] floats = (obj as NiFloatsExtraData).floatData;
            if (floats.Count() == 4)
            {
                Color c = mat.GetColor(name);
                if (c != null)
                {
                    Color color = new Color(floats[0], floats[1], floats[2], floats[3]);
                    mat.SetColor(name, color);
                    //Debug.Log("setting [" + name + "] of material to color: " + color);
                    return;
                }
            }
            mat.SetFloatArray(name, floats);
            //Debug.Log("setting [" + name + "] of material to " + string.Join(",", (obj as NiFloatsExtraData).floatData.Select(x => "" + x).ToArray()));
        }
        else if (obj is NiIntegerExtraData)
        {
            mat.SetInt(name, (obj as NiIntegerExtraData).intExtraData);
            //Debug.Log("setting [" + name + "] of material to " + (obj as NiIntegerExtraData).intExtraData);
        }
        else if (obj is NiBooleanExtraData)
        {
            int b = (obj as NiBooleanExtraData).booleanData ? 1 : 0;
            mat.SetInt(name, b);
            //Debug.Log("setting [" + name + "] of material to " + b);
        }
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

    static Dictionary<String, TextureData> texDataCache = new Dictionary<string, TextureData>();
    static Dictionary<String, Texture> toriginals = new Dictionary<string, Texture>();

    static public Texture getCachedTObject(string fn)
    {
        if (toriginals.ContainsKey(fn))
        {
            return toriginals[fn];
        }
        return null;
    }

    static private Texture loadTexture1(NIFFile file, string name)
    {
        Texture tex = getCachedTObject(name);
        if (tex != null)
            return tex;
        lock (texDataCache)
        {
            if (texDataCache.ContainsKey(name))
                tex = texDataCache[name].getTextureAndPurge();
        }
        if (tex == null)
        {
            try
            {
                byte[] data;
                AssetDatabase db = AssetDatabaseInst.DB;
                if (db == null)
                {
                    //Debug.Log("db was null");
                    return new Texture2D(2, 2);
                }
                data = db.extractUsingFilename(name, AssetDatabase.RequestCategory.TEXTURE);
                tex = DDSLoader.DatabaseLoaderTexture_DDS.LoadDDS(data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Unable to load texture:" + name + ":" + ex);
                tex = new Texture2D(2, 2);
            }
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

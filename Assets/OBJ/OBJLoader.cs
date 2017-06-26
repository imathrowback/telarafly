/*
(C) 2015 AARO4130
DO NOT USE PARTS OF, OR THE ENTIRE SCRIPT, AND CLAIM AS YOUR OWN WORK
*/

using System;

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Export
{
    public class OBJLoader
    {

        public static bool splitByMaterial = true;
        public static string[] searchPaths = new string[] { "", "%FileName%_Textures" + Path.DirectorySeparatorChar };

        public static Dictionary<String, GameObject> originals = new Dictionary<string, GameObject>();

        //structures
        public struct OBJFace
        {
            public string materialName;
            public string meshName;
            public int[] indexes;
        }


        public static Vector3 ParseVectorFromCMPS(string[] cmps)
        {
            float x = float.Parse(cmps[1]);
            float y = float.Parse(cmps[2]);
            if (cmps.Length == 4)
            {
                float z = float.Parse(cmps[3]);
                return new Vector3(x, y, z);
            }
            return new Vector2(x, y);
        }
        public static Color ParseColorFromCMPS(string[] cmps, float scalar = 1.0f)
        {
            float Kr = float.Parse(cmps[1]) * scalar;
            float Kg = float.Parse(cmps[2]) * scalar;
            float Kb = float.Parse(cmps[3]) * scalar;
            return new Color(Kr, Kg, Kb);
        }

        public static string OBJGetFilePath(string path, string basePath, string fileName)
        {
            foreach (string sp in searchPaths)
            {
                string s = sp.Replace("%FileName%", fileName);
                if (File.Exists(basePath + s + path))
                {
                    return basePath + s + path;
                }
                else if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }
        public static Material[] LoadMTLFile(string fn)
        {
            Material currentMaterial = null;
            List<Material> matlList = new List<Material>();
            FileInfo mtlFileInfo = new FileInfo(fn);
            string baseFileName = Path.GetFileNameWithoutExtension(fn);
            string mtlFileDirectory = mtlFileInfo.Directory.FullName + Path.DirectorySeparatorChar;
            foreach (string ln in File.ReadAllLines(fn))
            {
                string l = ln.Trim().Replace("  ", " ");
                string[] cmps = l.Split(' ');
                string data = l.Remove(0, l.IndexOf(' ') + 1);

                if (cmps[0] == "newmtl")
                {
                    if (currentMaterial != null)
                    {
                        matlList.Add(currentMaterial);
                    }
                    currentMaterial = new Material(Shader.Find("Legacy Shaders/Specular"));
                    currentMaterial.name = data;
                }
                else if (cmps[0] == "Kd")
                {
                    currentMaterial.SetColor("_Color", ParseColorFromCMPS(cmps));
                }
                else if (cmps[0] == "map_Kd")
                {
                    //TEXTURE
                    string fpth = OBJGetFilePath(data, mtlFileDirectory, baseFileName);
                    if (fpth != null)
                        currentMaterial.SetTexture("_MainTex", TextureLoader.LoadTexture(fpth));
                }
                else if (cmps[0] == "map_Bump")
                {
                    //TEXTURE
                    string fpth = OBJGetFilePath(data, mtlFileDirectory, baseFileName);
                    if (fpth != null)
                    {
                        currentMaterial.SetTexture("_BumpMap", TextureLoader.LoadTexture(fpth, true));
                        currentMaterial.EnableKeyword("_NORMALMAP");
                    }
                }
                else if (cmps[0] == "Ks")
                {
                    currentMaterial.SetColor("_SpecColor", ParseColorFromCMPS(cmps));
                }
                else if (cmps[0] == "Ka")
                {
                    currentMaterial.SetColor("_EmissionColor", ParseColorFromCMPS(cmps, 0.05f));
                    currentMaterial.EnableKeyword("_EMISSION");
                }
                else if (cmps[0] == "d")
                {
                    float visibility = float.Parse(cmps[1]);
                    if (visibility < 1)
                    {
                        Color temp = currentMaterial.color;

                        temp.a = visibility;
                        currentMaterial.SetColor("_Color", temp);

                        //TRANSPARENCY ENABLER
                        currentMaterial.SetFloat("_Mode", 3);
                        currentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        currentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        currentMaterial.SetInt("_ZWrite", 0);
                        currentMaterial.DisableKeyword("_ALPHATEST_ON");
                        currentMaterial.EnableKeyword("_ALPHABLEND_ON");
                        currentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        currentMaterial.renderQueue = 3000;
                    }

                }
                else if (cmps[0] == "Ns")
                {
                    float Ns = float.Parse(cmps[1]);
                    Ns = (Ns / 1000);
                    currentMaterial.SetFloat("_Glossiness", Ns);

                }
            }
            if (currentMaterial != null)
            {
                matlList.Add(currentMaterial);
            }
            return matlList.ToArray();
        }
        /*
        static List<Vector3> vertices = new List<Vector3>(1000);
        static List<Vector3> normals = new List<Vector3>(1000);
        static List<Vector2> uvs = new List<Vector2>(1000);
        //UMESH LISTS
        static List<Vector3> uvertices = new List<Vector3>(1000);
        static List<Vector3> unormals = new List<Vector3>(1000);
        static List<Vector2> uuvs = new List<Vector2>(1000);
        static List<OBJFace> faceList = new List<OBJFace>(1000);


        static  List<Vector3> processedVertices = new List<Vector3>();
        static List<Vector3> processedNormals = new List<Vector3>();
        static List<Vector2> processedUVs = new List<Vector2>();
        static  List<int[]> processedIndexes = new List<int[]>();

        public static void initprocessed()
        {
            processedVertices.Clear();
            processedNormals.Clear();
            processedUVs.Clear();
            processedIndexes.Clear();
        }

        public static void init()
        {
            vertices.Clear();
            normals.Clear();
            uvs.Clear();
            uvertices.Clear();
            unormals.Clear();
            uuvs.Clear();
            faceList.Clear();

        }*/

        public class PreparedMesh
        {
            public string meshName;
            public List<string> objectNames = new List<string>(20);
            public List<OBJFace> faceList = new List<OBJFace>();
            public List<string> materialNames = new List<string>(20);
            public Boolean hasNormals = false;
            public Material[] materialCache = null;
            public List<Vector3> uvertices = new List<Vector3>(1000);
            public List<Vector3> unormals = new List<Vector3>(1000);
            public List<Vector2> uuvs = new List<Vector2>(1000);
            internal PrepMesh2 pMesh2;
        }

        public static PreparedMesh prepare(string fn)
        {
            PreparedMesh prepMesh = new PreparedMesh();

            List<string> objectNames = prepMesh.objectNames;
            List<OBJFace> faceList = prepMesh.faceList;

            string meshName = Path.GetFileNameWithoutExtension(fn);

            //OBJ LISTS
            List<Vector3> vertices = new List<Vector3>(1000);
            List<Vector3> normals = new List<Vector3>(1000);
            List<Vector2> uvs = new List<Vector2>(1000);
            //UMESH LISTS

            //MESH CONSTRUCTION
            List<string> materialNames = prepMesh.materialNames;

            Dictionary<string, int> hashtable = new Dictionary<string, int>();

            string cmaterial = "";
            string cmesh = "default";
            //CACHE
            //save this info for later
            FileInfo OBJFileInfo = new FileInfo(fn);

            Debug.Log("cmesh:" + cmesh);
            Debug.Log("cmaterial:" + cmaterial);



            String ln = "";
            StreamReader reader = new StreamReader(fn);
            while ((ln = reader.ReadLine()) != null)
            {
                if (ln.Length > 0 && ln[0] != '#')
                {
                    string l = ln.Trim().Replace("  ", " ");
                    string[] cmps = l.Split(' ');
                    string data = l.Remove(0, l.IndexOf(' ') + 1);
                    //Debug.Log("parse: " + l);

                    if (cmps[0] == "mtllib")
                    {
                        //load cache
                        string pth = OBJGetFilePath(data, OBJFileInfo.Directory.FullName + Path.DirectorySeparatorChar, meshName);
                        if (pth != null)
                            prepMesh.materialCache = LoadMTLFile(pth);

                    }
                    else if ((cmps[0] == "g" || cmps[0] == "o") && splitByMaterial == false)
                    {
                        cmesh = data;
                        if (!objectNames.Contains(cmesh))
                        {
                            objectNames.Add(cmesh);
                        }
                    }
                    else if (cmps[0] == "usemtl")
                    {
                        cmaterial = data;
                        if (!materialNames.Contains(cmaterial))
                        {
                            materialNames.Add(cmaterial);
                        }

                        if (splitByMaterial)
                        {
                            if (!objectNames.Contains(cmaterial))
                            {
                                objectNames.Add(cmaterial);
                            }
                        }
                    }
                    else if (cmps[0] == "v")
                    {
                        //VERTEX
                        vertices.Add(ParseVectorFromCMPS(cmps));
                    }
                    else if (cmps[0] == "vn")
                    {
                        //VERTEX NORMAL
                        normals.Add(ParseVectorFromCMPS(cmps));
                    }
                    else if (cmps[0] == "vt")
                    {
                        //VERTEX UV
                        uvs.Add(ParseVectorFromCMPS(cmps));
                    }
                    else if (cmps[0] == "f")
                    {
                        int[] indexes = new int[cmps.Length - 1];
                        for (int i = 1; i < cmps.Length; i++)
                        {
                            string felement = cmps[i];
                            int vertexIndex = -1;
                            int normalIndex = -1;
                            int uvIndex = -1;
                            if (felement.Contains("//"))
                            {
                                //doubleslash, no UVS.
                                string[] elementComps = felement.Split('/');
                                vertexIndex = int.Parse(elementComps[0]) - 1;
                                normalIndex = int.Parse(elementComps[2]) - 1;
                            }
                            else if (felement.Count(x => x == '/') == 2)
                            {
                                //contains everything
                                string[] elementComps = felement.Split('/');
                                vertexIndex = int.Parse(elementComps[0]) - 1;
                                uvIndex = int.Parse(elementComps[1]) - 1;
                                normalIndex = int.Parse(elementComps[2]) - 1;
                            }
                            else if (!felement.Contains("/"))
                            {
                                //just vertex inedx
                                vertexIndex = int.Parse(felement) - 1;
                            }
                            else
                            {
                                //vertex and uv
                                string[] elementComps = felement.Split('/');
                                vertexIndex = int.Parse(elementComps[0]) - 1;
                                uvIndex = int.Parse(elementComps[1]) - 1;
                            }
                            string hashEntry = vertexIndex + "|" + normalIndex + "|" + uvIndex;
                            if (hashtable.ContainsKey(hashEntry))
                            {
                                indexes[i - 1] = hashtable[hashEntry];
                            }
                            else
                            {
                                //create a new hash entry
                                indexes[i - 1] = hashtable.Count;
                                hashtable[hashEntry] = hashtable.Count;
                                prepMesh.uvertices.Add(vertices[vertexIndex]);
                                if (normalIndex < 0 || (normalIndex > (normals.Count - 1)))
                                {
                                    prepMesh.unormals.Add(Vector3.zero);
                                }
                                else
                                {
                                    prepMesh.hasNormals = true;
                                    prepMesh.unormals.Add(normals[normalIndex]);
                                }
                                if (uvIndex < 0 || (uvIndex > (uvs.Count - 1)))
                                {
                                    prepMesh.uuvs.Add(Vector2.zero);
                                }
                                else
                                {
                                    prepMesh.uuvs.Add(uvs[uvIndex]);
                                }
                            }
                        }

                        if (indexes.Length < 5 && indexes.Length >= 3)
                        {


                            OBJFace f1 = new OBJFace();
                            f1.materialName = cmaterial;
                            f1.indexes = new int[] { indexes[0], indexes[1], indexes[2] };
                            f1.meshName = (splitByMaterial) ? cmaterial : cmesh;
                            faceList.Add(f1);
                            if (indexes.Length > 3)
                            {

                                OBJFace f2 = new OBJFace();
                                f2.materialName = cmaterial;
                                f2.meshName = (splitByMaterial) ? cmaterial : cmesh;
                                f2.indexes = new int[] { indexes[2], indexes[3], indexes[0] };
                                faceList.Add(f2);
                            }
                        }
                    }
                }
            }

            reader.Close();
            if (objectNames.Count == 0)
                objectNames.Add("default");

            prepMesh.pMesh2 = prepMesh2(prepMesh, fn);

            return prepMesh;
        }

        public static Boolean cacheExists(string fn)
        {
            return originals.ContainsKey(fn);
        }

        public static GameObject getCachedObject(string fn)
        {
            if (originals.ContainsKey(fn))
            {
                GameObject go = originals[fn];
                GameObject newG = GameObject.Instantiate(go);
                return newG;
            }
            return null;
        }

        public class SMesh
        {
            internal List<string> materialNames;
            internal List<int[]> pI;
            internal Vector3[] pN;
            internal Vector2[] pUV;
            internal Vector3[] pV;
        }

        public class PrepMesh2
        {
            public Dictionary<String, SMesh> subMeshes = new Dictionary<String, SMesh>();

        }

        public static PrepMesh2 prepMesh2(PreparedMesh prepMesh, String fn)
        {

            PrepMesh2 pMesh2 = new PrepMesh2();
            Debug.Log(prepMesh.objectNames.Count);

            foreach (string obj in prepMesh.objectNames)
            {
                SMesh sMesh = new SMesh();

                List<Vector3> processedVertices = new List<Vector3>();
                List<Vector3> processedNormals = new List<Vector3>();
                List<Vector2> processedUVs = new List<Vector2>();
                List<int[]> processedIndexes = new List<int[]>();
                Dictionary<int, int> remapTable = new Dictionary<int, int>();
                //POPULATE MESH
                List<string> meshMaterialNames = new List<string>();

                #region dosubmesh
                OBJFace[] ofaces = prepMesh.faceList.Where(x => x.meshName == obj).ToArray();
                Debug.Log(ofaces.Length);
                foreach (string mn in prepMesh.materialNames)
                {
                    OBJFace[] faces = ofaces.Where(x => x.materialName == mn).ToArray();
                    Debug.Log("[" + mn + "] faces: " + faces.Length);

                    if (faces.Length > 0)
                    {
                        int iSize = 0;
                        foreach (OBJFace f in faces)
                            iSize += f.indexes.Length;

                        List<int> indexes = new List<int>(iSize);
                        foreach (OBJFace f in faces)
                            indexes.AddRange(f.indexes);

                        processedVertices.Capacity += indexes.Count;
                        processedNormals.Capacity += indexes.Count;
                        processedUVs.Capacity += indexes.Count;

                        meshMaterialNames.Add(mn);

                        for (int i = 0; i < indexes.Count; i++)
                        {
                            int idx = indexes[i];
                            //build remap table
                            if (remapTable.ContainsKey(idx))
                            {
                                //ezpz
                                indexes[i] = remapTable[idx];
                            }
                            else
                            {
                                processedVertices.Add(prepMesh.uvertices[idx]);
                                processedNormals.Add(prepMesh.unormals[idx]);
                                processedUVs.Add(prepMesh.uuvs[idx]);
                                remapTable[idx] = processedVertices.Count - 1;
                                indexes[i] = remapTable[idx];
                            }
                        }

                        processedIndexes.Add(indexes.ToArray());
                    }
                    else
                    {

                    }
                }
                #endregion
                sMesh.pV = processedVertices.ToArray();
                sMesh.pN = processedNormals.ToArray();
                sMesh.pUV = processedUVs.ToArray();
                sMesh.pI = processedIndexes;
                sMesh.materialNames = meshMaterialNames;
                pMesh2.subMeshes[obj] = sMesh;
            }
            return pMesh2;
        }

        public static GameObject getGameObject(PreparedMesh prepMesh, String fn)
        {
            GameObject parentObject = new GameObject(prepMesh.meshName);


            foreach (string obj in prepMesh.objectNames)
            {
                GameObject subObject = new GameObject(obj);
                subObject.transform.parent = parentObject.transform;
                //Create mesh
                Mesh m = new Mesh();
                m.name = obj;
                Debug.Log("mesh " + m.name);

                SMesh pm2 = prepMesh.pMesh2.subMeshes[obj];
                var meshMaterialNames = pm2.materialNames;
                //apply stuff
                m.vertices = pm2.pV;
                m.normals = pm2.pN;
                m.uv = pm2.pUV;

                //Debug.Log("vertices:" + m.vertices.Count());
                m.subMeshCount = pm2.pI.Count;
                for (int i = 0; i < pm2.pI.Count; i++)
                {
                    m.SetTriangles(pm2.pI[i], i);
                }

                if (!prepMesh.hasNormals)
                {
                    m.RecalculateNormals();
                }
                m.RecalculateBounds();

                MeshFilter mf = subObject.AddComponent<MeshFilter>();
                MeshRenderer mr = subObject.AddComponent<MeshRenderer>();
                //MeshCollider mc = subObject.AddComponent<MeshCollider>();

                string PNG_DIR = Assets.ProgramSettings.get("PNG_TEXTURE_DIR");
                Shader basicBasic = Shader.Find("BasicShader");
                Shader basicTransparent = Shader.Find("TransparentBasicShader");
                Shader basicWaterTransparent = Shader.Find("WaterShader");
                Shader basicTerrain = Shader.Find("BasicTerrainShader");
                Shader basic = basicBasic;
                if (fn.Contains("terrain"))
                    basic = basicTerrain;

                Material[] processedMaterials = new Material[meshMaterialNames.Count];
                for (int i = 0; i < meshMaterialNames.Count; i++)
                {
                    string matName = meshMaterialNames[i];
                    string matNameL = meshMaterialNames[i].ToLower();
                    string[] transNames = { "leave", "leaf", "fern", "hedge", "foliage", "shrub", "ocean_chunk", "bush", "plants_combine" };
                    foreach (string tb in transNames)
                    {
                        if (matNameL.Contains(tb))
                        {
                            basic = basicTransparent;
                            break;
                        }
                    }
                    if (matNameL.Contains("ocean_chunk"))
                        basic = basicWaterTransparent;
                    if (prepMesh.materialCache == null)
                    {
                        processedMaterials[i] = new Material(basic);
                        processedMaterials[i].mainTexture = LoadTexture(PNG_DIR + matName);
                    }
                    else
                    {
                        Material mfn = Array.Find(prepMesh.materialCache, x => x.name == matName);
                        if (mfn == null)
                        {
                            processedMaterials[i] = new Material(basic);
                        }
                        else
                        {
                            processedMaterials[i] = mfn;
                        }

                    }
                    processedMaterials[i].mainTextureScale = new Vector2(1, -1);
                    processedMaterials[i].name = matName;
                }

                mr.materials = processedMaterials;
                mf.mesh = m;

                //mc.sharedMesh = m;
            }
            originals[fn] = parentObject;
            return parentObject;
        }

        public static Texture2D LoadTexture(string filePath)
        {
            Texture2D tex = null;
            try
            {
                // Debug.Log("loading texture " + filePath);
                if (File.Exists(filePath))
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    tex = new Texture2D(2, 2);
                    tex.LoadImage(fileData);

                    //tex = DDSLoader.DatabaseLoaderTexture_DDS.LoadDDS(filePath);
                    //if (tex == null)
                    //   Debug.LogError("tex error:" + DDSLoader.DatabaseLoaderTexture_DDS.error);
                    return tex;
                }
                else
                    Debug.Log("unable to loading texture " + filePath);
            }
            catch (Exception ex)
            {
                Debug.Log(filePath + ":" + ex);
            }
            return tex;
        }



    }
}
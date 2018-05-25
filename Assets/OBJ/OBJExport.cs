using Assets.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Export
{
    class OBJExport
    {
        public void export(GameObject root, string outputDir, string fileName, List<string> additionalComments)
        {
            String tDir = outputDir + "\\" + ExportModelData.outputDirectoryTextures;
            if (!Directory.Exists(tDir))
            {
                Debug.Log("Texture directory '" + tDir + "' does not exist, creating");
                Directory.CreateDirectory(tDir);
            }

            DB db = DBInst.inst;
            DoExport(root, true,  outputDir, fileName, additionalComments);

            



        }

        static void DoExport(GameObject obj, bool makeSubmeshes,  string outputDir, string fileName, List<string> additionalComments)
        {

            string meshName = obj.name;
            string mtllib =  outputDir + "\\" + fileName + ".mtl";

            ObjExporterScript.Start();
            string objout = outputDir + "\\" + fileName + ".obj";
            if (File.Exists(objout))
            {
                Debug.Log("skipping, already exists: " + objout);
                return;
            }
            using (StreamWriter sw = new StreamWriter(objout))
            {
                using (StreamWriter mtllibs = new StreamWriter(mtllib))
                {
                    sw.WriteLine("#" + meshName + ".obj");
                    sw.WriteLine("#" + System.DateTime.Now.ToLongDateString());
                    sw.WriteLine("#" + System.DateTime.Now.ToLongTimeString());
                    foreach (string s in additionalComments)
                        sw.WriteLine("#" + s);
                    sw.WriteLine("#-------");
                    sw.Write("\n\n");
                    sw.WriteLine("mtllib " + fileName + ".mtl");
                    Transform t = obj.transform;

                    Vector3 originalPosition = t.position;
                    t.position = Vector3.zero;

                    if (!makeSubmeshes)
                        sw.Write("g " + t.name + "\n");
                    processTransform(t, makeSubmeshes,  outputDir, mtllibs, sw);


                    t.position = originalPosition;

                    ObjExporterScript.End();
                    //Debug.Log("Exported Mesh: " + fileName);
                }
            }
        }

        static void processTransform(Transform t, bool makeSubmeshes,  string outputDir, StreamWriter  mtllib, StreamWriter sw)
        {
            HashSet<String> matsSet = new HashSet<string>();
            sw.Write("#" + t.name
                        + "\n#-------"
                        + "\n");

            if (makeSubmeshes)
                sw.Write("g " + t.name + "\n");

            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf)
            {
                sw.Write(ObjExporterScript.MeshToString(mf, t));
                // write materials
                Mesh m = mf.sharedMesh;
                if (m != null)
                {
                    Renderer ren = mf.GetComponent<Renderer>();
                    if (ren != null)
                    {
                        Material[] mats = ren.sharedMaterials;
                        for (int material = 0; material < m.subMeshCount; material++)
                        {
                            Material mat = mats[material];
                            if (mat != null)
                            {
                                string matName = mat.name;
                                Texture mainTex = null;
                                mainTex = mat.GetTexture("_MainTex");
                                if (mainTex != null)
                                {
                                    if (!matsSet.Contains(matName))
                                    {
                                        Texture2D t2d = (Texture2D)mainTex;
                                        byte[] tdata =Assets.AssetDatabaseInst.DB.extractUsingFilename(mainTex.name);

                                        string tname = outputDir + "\\" + ExportModelData.outputDirectoryTextures + "\\" + mainTex.name;
                                        
                                        if (!File.Exists(tname))
                                            File.WriteAllBytes(tname, tdata);

                                        mtllib.WriteLine("newmtl " + matName);
                                        mtllib.WriteLine("map_Kd " + ("\\" + ExportModelData.outputDirectoryTextures + "\\" + mainTex.name).Replace("dds", ExportModelData.expectedTextureExtension));
                                        //Texture tex = mat.mainTexture;

                                    }
                                }
                                matsSet.Add(matName);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < t.childCount; i++)
            {
                processTransform(t.GetChild(i), makeSubmeshes,  outputDir, mtllib ,sw);
            }
            
        }
    }
}

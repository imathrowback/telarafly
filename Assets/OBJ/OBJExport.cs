using Assets.Database;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Export
{
    class OBJExport
    {
        public void export(GameObject root, string outputDir, string fileName, List<string> additionalComments)
        {
            var textureDirectory = Path.Combine(outputDir, ExportModelData.outputDirectoryTextures);

            if (!Directory.Exists(textureDirectory))
            {
                Debug.Log("Texture directory '" + textureDirectory + "' does not exist, creating");
                Directory.CreateDirectory(textureDirectory);
            }

            DB db = DBInst.inst;
            DoExport(root, true, outputDir, fileName, additionalComments);
        }

        static void DoExport(GameObject obj, bool makeSubmeshes, string outputDir, string fileName, List<string> additionalComments)
        {
            string meshName = obj.name;
            string mtllib = Path.Combine(outputDir, fileName + ".mtl");
            string outputObject = Path.Combine(outputDir, fileName + ".obj");

            ObjExporterScript.Start();

            if (File.Exists(outputObject))
            {
                Debug.Log("skipping, already exists: " + outputObject);
                return;
            }

            using (StreamWriter sw = new StreamWriter(outputObject))
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
                    processTransform(t, makeSubmeshes, outputDir, mtllibs, sw);


                    t.position = originalPosition;

                    ObjExporterScript.End();
                    //Debug.Log("Exported Mesh: " + fileName);
                }
            }
        }

        static void processTransform(Transform t, bool makeSubmeshes, string outputDir, StreamWriter mtllib, StreamWriter sw)
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

                            if (mat == null)
                                continue;

                            string matName = mat.name;
                            Texture mainTex = null;
                            mainTex = mat.GetTexture("_MainTex");

                            // If the texture is valid, and not yet in the set
                            //  then extract the texture information and update the mtl file
                            if (mainTex != null && !matsSet.Contains(matName))
                            {
                                // Get the raw texture byte
                                byte[] textureData = AssetDatabaseInst.DB.extractUsingFilename(mainTex.name);

                                // Set the appropriate file name of the texture
                                var fileName = Path.ChangeExtension(mainTex.name, ExportModelData.expectedTextureExtension);

                                // Set the path where the texture file will be written
                                var textureOutputPath = Path.Combine(Path.Combine(outputDir, ExportModelData.outputDirectoryTextures), fileName);

                                if (!File.Exists(textureOutputPath))
                                {
                                    File.WriteAllBytes(textureOutputPath, textureData);
                                }

                                // Set the path of the texture file for the MTL file
                                var textureMtlPath = GetMtlTexturePath(ExportModelData.mtlWebPathing, ExportModelData.outputDirectoryTextures, fileName);

                                mtllib.WriteLine("newmtl " + matName);
                                mtllib.WriteLine("map_Kd " + textureMtlPath);
                            }

                            matsSet.Add(matName);
                        }
                    }
                }
            }

            for (int i = 0; i < t.childCount; i++)
            {
                processTransform(t.GetChild(i), makeSubmeshes, outputDir, mtllib, sw);
            }
        }

        static string GetMtlTexturePath(bool exportingToWeb, string textureDirectory, string filename)
        {
            return exportingToWeb ?
                string.Format("{0}/{1}", textureDirectory, filename) :
                Path.Combine(Path.Combine(Path.DirectorySeparatorChar.ToString(), textureDirectory), filename);
        }
    }
}

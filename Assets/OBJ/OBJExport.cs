using Assets.Database;
using Assets.WorldStuff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Export
{
    class OBJExport
    {
        public void export(GameObject root, string outputDir, string fileName, List<string> additionalComments)
        {
            //DB db = DBInst.inst;
            //DoExport(root, outputDir, fileName, additionalComments);

        }

        public void export(GameObject root)
        {
            DB db = DBInst.inst;
            DoExport(root, "export");

        }

        static void DoExport(GameObject obj, string rootOutputDir)
        {
            /** Move model to zero, then move it back once done */
            Transform t = obj.transform;
            Vector3 originalPosition = t.position;
            t.position = Vector3.zero;

            processTransform(t, rootOutputDir);

            t.position = originalPosition;

            ObjExporterScript.End();


        }


        static string SanitizeDir(String dir)
        {

            string invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            String fixedDir = Regex.Replace(dir, invalidRegStr, "_");
            fixedDir = fixedDir.Replace(":", "_");
            return fixedDir;
        }

        static void EnsureDirExists(String dir)
        {
            if (!Directory.Exists(dir))
            {
                Debug.Log("Directory '" + dir + "' does not exist, creating");
                Directory.CreateDirectory(dir);
            }
        }

        static string SanitizeFilename(String filename)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            String newName = Regex.Replace(filename, invalidRegStr, "_");
            newName = newName.Replace(":", "_");
            return newName;
        }

        static void processNIFToObj(Transform nifTransformRoot, string outputDir)
        {
            /** Process the children of this new object as a single OBJ */

            string meshName = SanitizeFilename(nifTransformRoot.name);
            string fileName = meshName;
            string mtllib = outputDir + "\\" + fileName + ".mtl";

            ObjExporterScript.Start();
            string objout = outputDir + "\\" + fileName + ".obj";
            EnsureDirExists(outputDir);

            HashSet<String> matsSet = new HashSet<string>();
            using (StreamWriter objFileStream = new(objout))
            {
                using (StreamWriter mtllibs = new(mtllib))
                {
                    for (int i = 0; i < nifTransformRoot.childCount; i++)
                    {
                        Transform t = nifTransformRoot.GetChild(i);
                        if (t.gameObject.activeInHierarchy)
                        {
                            MeshFilter mf = t.GetComponent<MeshFilter>();
                            if (mf)
                            {
                                // Top level model
                                objFileStream.Write("#" + t.name
                                            + "\n#-------"
                                            + "\n");
                                objFileStream.Write("o " + t.name);
                                objFileStream.Write(ObjExporterScript.MeshToString(mf, t));
                                objFileStream.WriteLine("mtllib " + fileName + ".mtl");
                                writeMaterials(mf, outputDir, mtllibs, matsSet);
                            }

                            //processNIFToObj(t, outputDir);
                        }
                    }
                }
            }
        }

        static void processTransform(Transform t, string initialOutputDir)
        {
            string outputDir = SanitizeDir(initialOutputDir);

            // Write each child recursively.. maybe?
            for (int i = 0; i < t.childCount; i++)
            {
                Transform trans = t.GetChild(i);
                if (trans.gameObject.activeInHierarchy)
                {
                    string newDir = outputDir + "\\" + SanitizeFilename(trans.name);

                    OriginalNIFReference oref = trans.gameObject.GetComponent<OriginalNIFReference>();
                    if (oref != null)
                    {
                        EnsureDirExists(newDir);
                        string outnif = newDir + "\\" + SanitizeFilename(oref.fname);

                        byte[] tdata = Assets.AssetDatabaseInst.DB.extractUsingFilename(oref.fname);
                        File.WriteAllBytes(outnif, tdata);

                        processNIFToObj(trans, newDir);
                    }
                    else
                        processTransform(trans, newDir);
                }
            }
        }


        private static void writeMaterials(MeshFilter mf, string outputDir, StreamWriter mtllib, HashSet<String> matsSet)
        {
           
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
                            string matName = mat.name + mat.GetInstanceID();
                            if (!matsSet.Contains(matName))
                            {
                                mtllib.WriteLine("newmtl " + matName);
                                writeTex(mat, mtllib, "_MainTex", "map_Kd", outputDir);
                                writeTex(mat, mtllib, "_MetallicGlossMap", "map_Ks", outputDir);
                                writeTex(mat, mtllib, "_BumpMap", "bump", outputDir);
                                writeTex(mat, mtllib, "_BumpMap", "map_bump", outputDir);


                                matsSet.Add(matName);
                            }
                        }
                    }
                }
            }
        }

        static void writeTex(Material mat, StreamWriter mtllib, string texName, string objText, string outputDir)
        {
            Texture mainTex = null;
            mainTex = mat.GetTexture(texName);
            if (mainTex != null)
            {
                String tDir = outputDir + "\\" + ExportModelData.outputDirectoryTextures;
                tDir = SanitizeDir(tDir);
                EnsureDirExists(tDir);

                Texture2D t2d = (Texture2D)mainTex;
                byte[] tdata = Assets.AssetDatabaseInst.DB.extractUsingFilename(mainTex.name);

                string relativetbaseName = "\\" + ExportModelData.outputDirectoryTextures + "\\" + mainTex.name;
                string tbaseName = "\\" + mainTex.name;
                string fulltname = tDir + tbaseName;
                string fullpngname = tDir + tbaseName.Replace("dds", "png");
                string relativepngname = relativetbaseName.Replace("dds", "png");

                if (!File.Exists(fulltname))
                {
                    File.WriteAllBytes(fulltname, tdata);
                    if (fulltname.EndsWith("dds"))
                        SaveTextureToFile(mainTex, fullpngname);
                }
                bool useDDS = false;
                // The textures are likely DDS. If they are, well.. that's unfortunate because Blender doesn't support texture painting with DDS.

                //mtllib.WriteLine(objText + " " + ("\\" + ExportModelData.outputDirectoryTextures + "\\" + mainTex.name).Replace("dds", ExportModelData.expectedTextureExtension));

                // DDS appears to be upside down, so if it's a PNG lets flip it (see also ObjExportScript :  sb.Append(string.Format("vt {0} {1}\n", v.x, -v.y))
                //if (useDDS)
                //{
                //    mtllib.WriteLine(objText + " " + relativetbaseName);
                //}
                //else
                //mtllib.WriteLine(objText + " -s u 1 -1 1 " + relativepngname);
                mtllib.WriteLine(objText + " " + relativepngname);
            }
        }



        public enum SaveTextureFileFormat
        {
            EXR,
            TGA,
            JPG,
            PNG

        };


        static public void SaveTextureToFile(Texture source,
                                             string filePath,
                                             int width = -1,
                                             int height = -1,
                                             bool flip = true,
                                             SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG,
                                             int jpgQuality = 95,
                                             bool asynchronous = true,
                                             System.Action<bool> done = null)
        {
            // check that the input we're getting is something we can handle:
            if (!(source is Texture2D || source is RenderTexture))
            {
                done?.Invoke(false);
                return;
            }

            // use the original texture size in case the input is negative:
            if (width < 0 || height < 0)
            {
                width = source.width;
                height = source.height;
            }

            // resize the original image:
            var resizeRT = RenderTexture.GetTemporary(width, height, 0);
            if (!flip)
                Graphics.Blit(source, resizeRT);
            else
                Graphics.Blit(source, resizeRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 1.0f));

            // create a native array to receive data from the GPU:
            var narray = new NativeArray<byte>(width * height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // request the texture data back from the GPU:
            var request = AsyncGPUReadback.RequestIntoNativeArray(ref narray, resizeRT, 0, (AsyncGPUReadbackRequest request) =>
            {
                // if the readback was successful, encode and write the results to disk
                if (!request.hasError)
                {
                    NativeArray<byte> encoded;

                    switch (fileFormat)
                    {
                        case SaveTextureFileFormat.EXR:
                            encoded = ImageConversion.EncodeNativeArrayToEXR(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                        case SaveTextureFileFormat.JPG:
                            encoded = ImageConversion.EncodeNativeArrayToJPG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height, 0, jpgQuality);
                            break;
                        case SaveTextureFileFormat.TGA:
                            encoded = ImageConversion.EncodeNativeArrayToTGA(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                        default:
                            encoded = ImageConversion.EncodeNativeArrayToPNG(narray, resizeRT.graphicsFormat, (uint)width, (uint)height);
                            break;
                    }

                    System.IO.File.WriteAllBytes(filePath, encoded.ToArray());
                    encoded.Dispose();
                }

                narray.Dispose();

                // notify the user that the operation is done, and its outcome.
                done?.Invoke(!request.hasError);
            });

            if (!asynchronous)
                request.WaitForCompletion();
        }
    }
}

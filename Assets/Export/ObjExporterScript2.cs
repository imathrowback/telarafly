using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Assuming Assets.Database and Assets.WorldStuff are now provided by your project.
// If you encounter compilation errors, ensure these namespaces and their types are accessible.

namespace Assets.Export
{
    // Constants for export paths and formats
    public static class ExportModelData
    {
        public const string outputDirectoryTextures = "textures";
        public const string expectedTextureExtension = "png"; // Always export textures as PNG
    }

    /// <summary>
    /// Helper class to convert Unity Mesh data into OBJ format strings.
    /// Handles vertex, normal, UV, and face data, including coordinate system transformations.
    /// </summary>
    public static class ObjExporterScript2 // Renamed from ObjExporterScript
    {
        // OBJ indices are 1-based. These offsets track the starting index for the current mesh.
        private static int currentVertexOffset = 1;
        private static int currentNormalOffset = 1;
        private static int currentUVOffset = 1;

        /// <summary>
        /// Initializes the exporter, resetting internal counters for a new export operation.
        /// Call this once before starting a new OBJ export.
        /// </summary>
        public static void Start()
        {
            currentVertexOffset = 1;
            currentNormalOffset = 1;
            currentUVOffset = 1;
        }

        /// <summary>
        /// Finalizes the exporter (no specific cleanup needed for this simplified version).
        /// Call this once after completing an OBJ export.
        /// </summary>
        public static void End()
        {
            // No specific cleanup needed here for this simplified version
        }

        /// <summary>
        /// Converts a Unity MeshFilter's mesh data into an OBJ format string.
        /// Applies coordinate system transformations (Unity Left-Handed to OBJ Right-Handed).
        /// </summary>
        /// <param name="mf">The MeshFilter containing the mesh to export.</param>
        /// <param name="t">The Transform of the GameObject associated with the MeshFilter, used for world space transformations.</param>
        /// <param name="uniqueMaterialNameTracker">A dictionary to track and generate unique material names.</param>
        /// <returns>A string containing the OBJ data for the mesh.</returns>
        public static string MeshToString(MeshFilter mf, Transform t, Dictionary<string, int> uniqueMaterialNameTracker)
        {
            Mesh m = mf.sharedMesh;
            // Get the Renderer component to access materials
            Renderer renderer = mf.GetComponent<Renderer>();
            Material[] mats = (renderer != null) ? renderer.sharedMaterials : new Material[0];

            StringBuilder sb = new StringBuilder();

            // Write Vertices (v)
            foreach (Vector3 v in m.vertices)
            {
                // Transform vertex from local space to world space.
                // Since the root object is temporarily moved to Vector3.zero,
                // t.TransformPoint(v) gives the vertex's position relative to the export origin.
                Vector3 world_v = t.TransformPoint(v);

                // Unity (Left-Handed, Y-up, Z-forward) to OBJ (Right-Handed, Y-up, Z-back) conversion:
                // Typically, negate the Z-coordinate. Some tools might also require negating X.
                // Negating Z is the most common and generally works well.
                sb.Append(string.Format("v {0:F6} {1:F6} {2:F6}\n", world_v.x, world_v.y, -world_v.z));
            }

            // Write Normals (vn)
            foreach (Vector3 n in m.normals)
            {
                // Transform normal from local space to world space (direction only).
                Vector3 world_n = t.TransformDirection(n);
                // Apply the same Z-negation as for vertices for consistency.
                sb.Append(string.Format("vn {0:F6} {1:F6} {2:F6}\n", world_n.x, world_n.y, -world_n.z));
            }

            // Write UVs (vt)
            foreach (Vector2 uv in m.uv)
            {
                // OBJ UVs (vt) typically have the V (Y) coordinate flipped (1.0 - uv.y)
                // compared to Unity's convention (which is often OpenGL/DirectX style).
                sb.Append(string.Format("vt {0:F6} {1:F6}\n", uv.x, 1.0f - uv.y));
            }

            // Write Faces (f) for each submesh/material
            for (int materialIndex = 0; materialIndex < m.subMeshCount; materialIndex++)
            {
                // Reference the material for this submesh
                if (materialIndex < mats.Length && mats[materialIndex] != null)
                {
                    // Generate a sanitized and unique material name using the helper function
                    string matName = ExporterV2.GetCleanUniqueMaterialName(mats[materialIndex], uniqueMaterialNameTracker);
                    sb.Append($"usemtl {matName}\n");
                }
                else
                {
                    // Fallback to a default material if a material is missing
                    sb.Append("usemtl default_material\n");
                }

                int[] triangles = m.GetTriangles(materialIndex);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    // OBJ faces are 1-indexed. Add the current offsets to the Unity 0-indexed triangle indices.
                    // The winding order is important. If negating Z for vertices flips the winding,
                    // the order (triangles[i], triangles[i+1], triangles[i+2]) should be correct.
                    // If issues arise, try reversing the order: (triangles[i+2], triangles[i+1], triangles[i]).
                    sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                        triangles[i] + currentVertexOffset,
                        triangles[i + 1] + currentVertexOffset,
                        triangles[i + 2] + currentVertexOffset));
                }
            }

            // Update global offsets for the next mesh that will be written to the OBJ file.
            // This ensures that subsequent meshes use correct 1-based indices.
            currentVertexOffset += m.vertices.Length;
            currentNormalOffset += m.normals.Length;
            currentUVOffset += m.uv.Length;

            return sb.ToString();
        }
    }

    /// <summary>
    /// Exports a Unity GameObject hierarchy to an OBJ file with associated MTL and textures.
    /// </summary>
    class ExporterV2 // Class name changed from OBJExport to ExporterV2
    {
        /// <summary>
        /// Internal struct to pass common export parameters through recursive calls.
        /// </summary>
        private struct ExportContext
        {
            public StreamWriter objWriter;
            public StreamWriter mtlWriter;
            public HashSet<string> writtenMaterials; // Tracks materials already written to MTL
            public string outputDir; // Base output directory for OBJ/MTL
            public string mtlFileName; // Name of the .mtl file (e.g., "model.mtl")
            public Dictionary<string, int> materialNameCounts; // Tracks base material names for unique suffixing
        }

        /// <summary>
        /// Exports a Unity GameObject tree to an OBJ file with specified output details.
        /// </summary>
        /// <param name="root">The root GameObject of the hierarchy to export.</param>
        /// <param name="outputDir">The directory where the OBJ, MTL, and textures will be saved.</param>
        /// <param name="fileName">The base name for the OBJ and MTL files (e.g., "myModel").</param>
        /// <param name="additionalComments">Optional list of comments to add to the OBJ file header.</param>
        public void export(GameObject root, string outputDir, string fileName, List<string> additionalComments = null)
        {
            DoExport(root, outputDir, fileName, additionalComments);
        }

        /// <summary>
        /// Exports a Unity GameObject tree to an OBJ file using default settings.
        /// Output will be in an "export" directory, with the file name derived from the root GameObject's name.
        /// </summary>
        /// <param name="root">The root GameObject of the hierarchy to export.</param>
        public void export(GameObject root)
        {
            // Default export to "export" directory with root object's name
            DoExport(root, "export", SanitizeFilename(root.name));
        }

        /// <summary>
        /// The core export logic. Handles file creation, context setup, and recursive processing.
        /// </summary>
        /// <param name="rootObj">The root GameObject to start the export from.</param>
        /// <param name="rootOutputDir">The base output directory.</param>
        /// <param name="fileName">The base file name for the OBJ/MTL.</param>
        /// <param name="additionalComments">Optional comments for the OBJ header.</param>
        static void DoExport(GameObject rootObj, string rootOutputDir, string fileName, List<string> additionalComments = null) // Made additionalComments optional
        {
            if (rootObj == null)
            {
                Debug.LogError("OBJ Export failed: Root GameObject is null.");
                return;
            }

            Debug.Log($"Starting OBJ export for: '{rootObj.name}' to '{rootOutputDir}\\{fileName}.obj'");

            // Ensure the root output directory exists and is sanitized
            string fullOutputDir = SanitizeDir(rootOutputDir);
            EnsureDirExists(fullOutputDir);

            string objFilePath = Path.Combine(fullOutputDir, fileName + ".obj");
            string mtlFilePath = Path.Combine(fullOutputDir, fileName + ".mtl");
            string mtlFileName = fileName + ".mtl"; // Just the file name for OBJ reference

            // Temporarily move the root object to origin and reset its rotation/scale for consistent export.
            // This ensures all child mesh vertices are calculated relative to (0,0,0) in world space.
            Transform originalParent = rootObj.transform.parent;
            Vector3 originalPosition = rootObj.transform.position;
            Quaternion originalRotation = rootObj.transform.rotation;
            Vector3 originalScale = rootObj.transform.localScale;

            // Detach from parent, move to origin, reset rotation/scale
            rootObj.transform.SetParent(null);
            rootObj.transform.position = Vector3.zero;
            rootObj.transform.rotation = Quaternion.identity;
            rootObj.transform.localScale = Vector3.one;

            // Initialize the ObjExporterScript's internal counters for a new export
            ObjExporterScript2.Start(); // Updated class name

            // Use 'using' statements to ensure file streams are properly closed
            using (StreamWriter objFileStream = new StreamWriter(objFilePath, false, Encoding.ASCII))
            using (StreamWriter mtlFileStream = new StreamWriter(mtlFilePath, false, Encoding.ASCII))
            {
                // Write OBJ file header and reference the MTL library
                objFileStream.WriteLine("# Unity OBJ Export by Gemini");
                objFileStream.WriteLine($"# Exported from Unity GameObject: {rootObj.name}");
                objFileStream.WriteLine($"# Date: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                if (additionalComments != null)
                {
                    foreach (string comment in additionalComments)
                    {
                        objFileStream.WriteLine($"# {comment}");
                    }
                }
                objFileStream.WriteLine($"mtllib {mtlFileName}");
                objFileStream.WriteLine(""); // Blank line for readability

                // Setup the export context to pass to recursive functions
                ExportContext context = new ExportContext
                {
                    objWriter = objFileStream,
                    mtlWriter = mtlFileStream,
                    writtenMaterials = new HashSet<string>(), // To track materials already written to MTL
                    outputDir = fullOutputDir,
                    mtlFileName = mtlFileName,
                    materialNameCounts = new Dictionary<string, int>() // Initialize for unique material naming
                };

                // Start the recursive processing from the root object
                ProcessGameObject(rootObj.transform, context);
            }

            // Finalize the ObjExporterScript (if any cleanup needed)
            ObjExporterScript2.End(); // Updated class name

            // Restore the original transform of the root object
            rootObj.transform.position = originalPosition;
            rootObj.transform.rotation = originalRotation;
            rootObj.transform.localScale = originalScale;
            if (originalParent != null)
            {
                rootObj.transform.SetParent(originalParent);
            }


            Debug.Log($"OBJ export complete: '{objFilePath}'");
        }

        /// <summary>
        /// Recursively processes a GameObject and its children, exporting meshes and materials.
        /// </summary>
        /// <param name="currentTransform">The current Transform in the hierarchy being processed.</param>
        /// <param name="context">The export context containing file writers and material tracking.</param>
        static void ProcessGameObject(Transform currentTransform, ExportContext context)
        {
            // Only process active GameObjects in the hierarchy
            if (!currentTransform.gameObject.activeInHierarchy)
            {
                return;
            }

            // Check if the current GameObject has a MeshFilter and a Renderer
            MeshFilter meshFilter = currentTransform.GetComponent<MeshFilter>();
            Renderer renderer = currentTransform.GetComponent<Renderer>();

            // If a mesh and renderer are found, export its data
            if (meshFilter != null && meshFilter.sharedMesh != null && renderer != null)
            {
                // Write object/group name to OBJ file for organization
                // Using 'g' for group and 'o' for object is common practice.
                context.objWriter.WriteLine($"g {currentTransform.name}");
                context.objWriter.WriteLine($"o {currentTransform.name}");

                // Get the mesh data string from ObjExporterScript and write it to the OBJ file
                // The ObjExporterScript handles vertex, normal, UV, and face data,
                // and includes 'usemtl' directives for submeshes.
                context.objWriter.Write(ObjExporterScript2.MeshToString(meshFilter, currentTransform, context.materialNameCounts)); // Updated class name

                // Write material definitions to the MTL file if they haven't been written yet
                WriteMaterials(meshFilter, context.outputDir, context.mtlWriter, context.writtenMaterials, context.materialNameCounts);
            }

            // Recursively call this method for all children of the current GameObject
            for (int i = 0; i < currentTransform.childCount; i++)
            {
                ProcessGameObject(currentTransform.GetChild(i), context);
            }
        }

        /// <summary>
        /// Sanitizes a directory path by replacing invalid characters with underscores.
        /// </summary>
        /// <param name="dir">The directory path to sanitize.</param>
        /// <returns>The sanitized directory path.</returns>
        public static string SanitizeDir(String dir)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            String fixedDir = Regex.Replace(dir, invalidRegStr, "_");
            fixedDir = fixedDir.Replace(":", "_"); // Replace colon specifically as it's common in paths
            return fixedDir;
        }

        /// <summary>
        /// Ensures that a specified directory exists, creating it if it doesn't.
        /// </summary>
        /// <param name="dir">The directory path to check/create.</param>
        static void EnsureDirExists(String dir)
        {
            if (!Directory.Exists(dir))
            {
                Debug.Log($"Directory '{dir}' does not exist, creating.");
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// Sanitizes a filename by replacing invalid characters with underscores.
        /// </summary>
        /// <param name="filename">The filename to sanitize.</param>
        /// <returns>The sanitized filename.</returns>
        public static string SanitizeFilename(String filename)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            String newName = Regex.Replace(filename, invalidRegStr, "_");
            newName = newName.Replace(":", "_"); // Replace colon specifically
            return newName;
        }

        /// <summary>
        /// Generates a clean and unique material name, removing "(Clone)" and adding a sequential suffix if needed.
        /// </summary>
        /// <param name="mat">The Material to get the name for.</param>
        /// <param name="uniqueNameTracker">A dictionary to track base names and their counts for uniqueness.</param>
        /// <returns>A unique and sanitized material name.</returns>
        public static string GetCleanUniqueMaterialName(Material mat, Dictionary<string, int> uniqueNameTracker)
        {
            string baseName = mat.name;

            // Remove "(Clone)" suffix if present
            int cloneIndex = baseName.LastIndexOf("(Clone)");
            if (cloneIndex != -1)
            {
                baseName = baseName.Substring(0, cloneIndex);
            }

            // Sanitize the base name
            baseName = SanitizeFilename(baseName);

            // Ensure uniqueness by appending a counter if needed
            string uniqueName = baseName;
            if (uniqueNameTracker.ContainsKey(baseName)) // Check for the base name, not the unique name
            {
                uniqueNameTracker[baseName]++;
                uniqueName = $"{baseName}_{uniqueNameTracker[baseName]}";
            }
            else
            {
                uniqueNameTracker.Add(baseName, 0); // Start count at 0 for the first instance
            }

            return uniqueName;
        }


        /// <summary>
        /// Writes material definitions to the MTL file. Ensures each material is written only once.
        /// </summary>
        /// <param name="mf">The MeshFilter whose materials are to be written.</param>
        /// <param name="outputDir">The base output directory.</param>
        /// <param name="mtllibWriter">The StreamWriter for the MTL file.</param>
        /// <param name="matsSet">A HashSet to track materials that have already been written.</param>
        /// <param name="uniqueMaterialNameTracker">A dictionary to track and generate unique material names.</param>
        private static void WriteMaterials(MeshFilter mf, string outputDir, StreamWriter mtllibWriter, HashSet<String> matsSet, Dictionary<string, int> uniqueMaterialNameTracker)
        {
            Mesh m = mf.sharedMesh;
            if (m == null) return;

            Renderer ren = mf.GetComponent<Renderer>();
            if (ren == null) return;

            Material[] mats = ren.sharedMaterials;
            for (int materialIndex = 0; materialIndex < m.subMeshCount; materialIndex++)
            {
                // Safety check for material array bounds
                if (materialIndex >= mats.Length) continue;

                Material mat = mats[materialIndex];
                if (mat != null)
                {
                    // Generate a unique and sanitized name for the material using the helper function
                    string matName = GetCleanUniqueMaterialName(mat, uniqueMaterialNameTracker);
                    if (!matsSet.Contains(matName))
                    {
                        mtllibWriter.WriteLine($"newmtl {matName}");
                        // Common material properties (adjust as needed for your specific materials)
                        Color color = mat.HasProperty("_Color") ? mat.color : Color.white;
                        mtllibWriter.WriteLine($"Kd {color.r:F6} {color.g:F6} {color.b:F6}"); // Diffuse color
                        mtllibWriter.WriteLine($"Ka {color.r:F6} {color.g:F6} {color.b:F6}"); // Ambient color (often same as diffuse)
                        mtllibWriter.WriteLine($"Ks 0.500000 0.500000 0.500000"); // Specular color (placeholder)
                        mtllibWriter.WriteLine($"Ns 96.078431"); // Specular exponent (shininess, placeholder)
                        mtllibWriter.WriteLine($"d 1.000000"); // Dissolve (opacity)
                        mtllibWriter.WriteLine($"illum 2"); // Illumination model (2 = diffuse + specular)

                        // Write texture references for common texture types
                        WriteTextureReference(mat, mtllibWriter, "_MainTex", "map_Kd", outputDir); // Diffuse map
                        WriteTextureReference(mat, mtllibWriter, "_MetallicGlossMap", "map_Ks", outputDir); // Specular map (often metallic/gloss)
                        WriteTextureReference(mat, mtllibWriter, "_BumpMap", "bump", outputDir); // Bump map (old convention)
                        WriteTextureReference(mat, mtllibWriter, "_NormalMap", "norm", outputDir); // Normal map (newer convention)
                        WriteTextureReference(mat, mtllibWriter, "_OcclusionMap", "map_ao", outputDir); // Ambient Occlusion map

                        mtllibWriter.WriteLine(""); // Blank line for readability
                        matsSet.Add(matName); // Add to set to prevent re-writing
                    }
                }
            }
        }

        /// <summary>
        /// Writes a texture reference to the MTL file and exports the texture image.
        /// </summary>
        /// <param name="mat">The Material containing the texture.</param>
        /// <param name="mtllibWriter">The StreamWriter for the MTL file.</param>
        /// <param name="unityTexPropertyName">The Unity shader property name for the texture (e.g., "_MainTex").</param>
        /// <param name="objMapType">The OBJ/MTL map type (e.g., "map_Kd", "bump").</param>
        /// <param name="outputDir">The base output directory.</param>
        static void WriteTextureReference(Material mat, StreamWriter mtllibWriter, string unityTexPropertyName, string objMapType, string outputDir)
        {
            Texture mainTex = mat.GetTexture(unityTexPropertyName);
            if (mainTex != null)
            {
                // Construct the texture file name and paths
                string textureFileName = SanitizeFilename(mainTex.name) + "." + ExportModelData.expectedTextureExtension;
                string textureOutputDir = Path.Combine(outputDir, ExportModelData.outputDirectoryTextures);
                EnsureDirExists(textureOutputDir); // Ensure texture output directory exists

                string fullTexturePath = Path.Combine(textureOutputDir, textureFileName);
                // Relative path for the MTL file
                string relativeTexturePath = Path.Combine(ExportModelData.outputDirectoryTextures, textureFileName);

                // Check if the texture file already exists to avoid re-exporting
                if (!File.Exists(fullTexturePath))
                {
                    // Save the Unity Texture to a file (e.g., PNG)
                    SaveTextureToFile(mainTex, fullTexturePath,
                                      fileFormat: SaveTextureFileFormat.PNG, // Force PNG export
                                      flip: true, // Flip V for OBJ UV compatibility
                                      asynchronous: false); // Make it synchronous for simpler export flow
                    Debug.Log($"Exported texture: '{fullTexturePath}'");
                }
                else
                {
                    Debug.Log($"Texture already exists, skipping export: '{fullTexturePath}'");
                }

                // Write the texture reference line to the MTL file
                mtllibWriter.WriteLine($"{objMapType} {relativeTexturePath}");
            }
        }

        // Enum for supported texture file formats
        public enum SaveTextureFileFormat
        {
            EXR,
            TGA,
            JPG,
            PNG
        };

        /// <summary>
        /// Saves a Unity Texture (Texture2D or RenderTexture) to a specified file path.
        /// This method reads pixels from the GPU and encodes them into the desired image format.
        /// </summary>
        /// <param name="source">The source Texture to save.</param>
        /// <param name="filePath">The full path where the image file will be saved.</param>
        /// <param name="width">Optional: Desired width of the saved image. If -1, uses source width.</param>
        /// <param name="height">Optional: Desired height of the saved image. If -1, uses source height.</param>
        /// <param name="flip">If true, flips the image vertically (useful for UV coordinate systems).</param>
        /// <param name="fileFormat">The desired file format (PNG, JPG, EXR, TGA).</param>
        /// <param name="jpgQuality">JPG quality (0-100) if JPG format is chosen.</param>
        /// <param name="asynchronous">If true, performs the readback asynchronously. For export, synchronous is often preferred.</param>
        /// <param name="done">Callback action indicating success/failure.</param>
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
            if (!(source is Texture2D || source is RenderTexture))
            {
                Debug.LogError($"SaveTextureToFile: Unsupported texture type for saving: {source.GetType().Name}. Path: {filePath}");
                done?.Invoke(false);
                return;
            }

            if (width < 0 || height < 0)
            {
                width = source.width;
                height = source.height;
            }

            RenderTexture resizeRT = null;
            Texture2D tempTexture2D = null;

            try
            {
                // Create a temporary RenderTexture to blit the source texture onto.
                // This handles resizing and flipping if needed, and ensures we can read pixels.
                resizeRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                if (!flip)
                    Graphics.Blit(source, resizeRT);
                else
                    // Blit with a flipped UV matrix to flip the image vertically
                    Graphics.Blit(source, resizeRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 1.0f));

                // Set the active RenderTexture to read from
                RenderTexture.active = resizeRT;

                // Create a temporary Texture2D to read the pixels from the RenderTexture
                tempTexture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tempTexture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tempTexture2D.Apply(); // Apply changes to the texture

                // Release the temporary RenderTexture immediately after reading
                RenderTexture.ReleaseTemporary(resizeRT);
                RenderTexture.active = null; // Clear active RenderTexture

                byte[] bytes = null;
                // Encode the Texture2D to the desired image format
                switch (fileFormat)
                {
                    case SaveTextureFileFormat.EXR:
                        bytes = ImageConversion.EncodeToEXR(tempTexture2D);
                        break;
                    case SaveTextureFileFormat.JPG:
                        bytes = ImageConversion.EncodeToJPG(tempTexture2D, jpgQuality);
                        break;
                    case SaveTextureFileFormat.TGA:
                        bytes = ImageConversion.EncodeToTGA(tempTexture2D);
                        break;
                    default: // PNG
                        bytes = ImageConversion.EncodeToPNG(tempTexture2D);
                        break;
                }

                if (bytes != null)
                {
                    File.WriteAllBytes(filePath, bytes);
                    done?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"SaveTextureToFile: Failed to encode texture to bytes for '{filePath}'.");
                    done?.Invoke(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveTextureToFile: Error saving texture to file '{filePath}': {e.Message}");
                done?.Invoke(false);
            }
            finally
            {
                // Ensure temporary Texture2D is destroyed to prevent memory leaks
                if (tempTexture2D != null)
                {
                    UnityEngine.Object.DestroyImmediate(tempTexture2D);
                }
                // Ensure temporary RenderTexture is released if it wasn't already (though it should be)
                if (resizeRT != null)
                {
                    RenderTexture.ReleaseTemporary(resizeRT);
                }
            }
        }
    }
}

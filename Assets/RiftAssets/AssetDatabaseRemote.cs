using Assets;
using Assets.RiftAssets;
using Org.BouncyCastle.Asn1.Ocsp;
using SevenZip;
using SharpCompress.Common;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors.Xz;
using SharpCompress.Readers;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static System.Net.WebRequestMethods;
public class AssetDatabaseRemote : AssetDatabase
{
   // string baseURL = "http://rift-update.dyn.triongames.com/ch1-live-streaming-client-patch/content/patchlive05/";

    public AssetDatabaseRemote(Manifest manifest) : base(manifest)
    {


    }

    public override bool isRemote()
    {
        return true;
    }


    public override byte[] extractUsingFilename(string filename, RequestCategory requestCategory = RequestCategory.NONE)
    {
        // Get PAK name for the given filename
        // Concat it with the base URL
        // Extract the PAK from the URL using the byte range

        Manifest manifest = getManifest();

        string id = getID(filename, requestCategory);
        ManifestEntry entry = manifest.getEntry(id);

        string cacheFile = Path.GetTempPath() + "rift/" + id;

        if (System.IO.File.Exists(cacheFile))
        {
            //Debug.Log("Cache hit for [" + filename + "]["  + id + "] at " + cacheFile);
            return System.IO.File.ReadAllBytes(cacheFile);
        }


        //List<ManifestEntry> entries = manifest.getEntriesForID(id);


        int pakIndex = entry.pakIndex;
        string pakName = manifest.getPAKName(pakIndex);

        string baseURL = ProgramSettings.get("REMOTE_ASSETS_URL", "");
        string newURL = baseURL + pakName;

        byte[] returnData = null;
        int startBytes = entry.pakOffset;
        int endBytes = startBytes + entry.compressedSize - 1; // inclusive end byte

        if (startBytes == 0 && endBytes == -1)
        {
            Debug.Log("Bad range for " + filename + ". returning empty byte array for entry: " + entry.idStr + "\n" + "Manifest Entry: " + entry.ToString() + "PAK origin: " + pakName);
            return new byte[0];
            //throw new Exception("Invalid byte range for entry: " + entry.idStr + " with start: " + startBytes + " and end: " + endBytes);
        }

        string range = "bytes=" + startBytes + "-" + endBytes;
        Debug.Log("Downloading [" + filename + "] for id[" + id + "] from URL: " + newURL + " with range: " + range);
        using (var client = new System.Net.Http.HttpClient())
        {
            client.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(startBytes, endBytes);
            var response = client.GetAsync(newURL).Result;
            if (response.IsSuccessStatusCode)
            {
                byte[] responseBytes = response.Content.ReadAsByteArrayAsync().Result;
                // if the size of the response is not equal to the compressed size, then use LZMA2 to decompress it and return the decompressed bytes
                if (entry.size != entry.compressedSize)
                {
                    // Debug.LogWarning("Entry size (" + entry.size + ") does not match expected compressed size (" + entry.compressedSize + "), decompressing using LZMA2 -- response bytes:" + responseBytes.Length);

                    MemoryStream compressedStream = new MemoryStream(responseBytes);

                    byte[] properties = new byte[1];
                    compressedStream.Read(properties, 0, 1); // Read the first byte for LZMA properties
                    int inputSize = responseBytes.Length -1 ; // This should be set to the size of the input data, if known
                    int outputSize = entry.size; // This is the size of the expected decompressed data
                    using (LzmaStream lzmaStream = new LzmaStream(properties, compressedStream, inputSize, outputSize, null, true))
                    {
                        MemoryStream outS = new MemoryStream();

                        lzmaStream.CopyTo(outS);

                        returnData = outS.ToArray();
                    }






                }
                else returnData = responseBytes;
            }
            else
            {
                throw new Exception("Failed to download asset from " + newURL + ": " + response.ReasonPhrase);
            }
        }

        System.IO.File.WriteAllBytes(cacheFile, returnData);
        return returnData;
    }



}

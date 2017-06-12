using Assets;
using Assets.DB;
using Assets.RiftAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ionic.Zlib;
using System.IO;
using XZ.NET;
using SevenZip;

public class FirstInitialLoader : MonoBehaviour {

    System.Threading.Thread loadThread;
    string progress;
    public Text text;

    // Use this for initialization
    void Start () {

        string f = @"L:\downloads\geometry_ep2_5.pak.lzma2";
        f = @"L:\downloads\english_core_0.pak";
        byte[] data = File.ReadAllBytes(f);
        MemoryStream s = new MemoryStream(data);
        s.Seek(30017, SeekOrigin.Begin);
        //XZ.NET.XZInputStream ins = new XZInputStream(s);
        //XZ.NET.XZInputStream.Decode(data);
        //ins.ReadByte();

        //LzmaDecodeStream lstr = new LzmaDecodeStream(s);
        //SevenZip.Sdk.Compression.Lzma.Decoder d = new SevenZip.Sdk.Compression.Lzma.Decoder();
        //d.
        //SevenZipCompressor c = new SevenZipCompressor();
        //c.CompressionMode = SevenZip.CompressionMode.
        SevenZipExtractor.DecompressStream(s, new MemoryStream(), data.Length, null);
        //SevenZipExtractor.ExtractBytes(data);
        //lstr.ReadByte();
        /*
    progress = "Starting load thread";
        AssetDatabaseInst.progressUpdate += (s) => progress = s;
        // start a loader thread
        loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(loadDatabase));
        loadThread.Start();
        */
    }

    public void loadDatabase()
    {
        try
        {
            Debug.LogWarning("start");
            progress = "Loading manifest and assets";

            AssetDatabase adb = AssetDatabaseInst.DB;

            string expectedChecksum = adb.getHash("telara.db");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
            progress = "" + ex;
        }
    }
	
	// Update is called once per frame
	void Update () {
        text.text = progress;
	}
}

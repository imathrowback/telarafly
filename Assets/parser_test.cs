using Assets;
using Assets.DatParser;
using Assets.RiftAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class parser_test : MonoBehaviour {
    int c = 0;
    byte[] data;
	// Use this for initialization
	void Start () {
        AssetDatabase adb = AssetDatabaseInst.DB;
        data = adb.extractUsingFilename("world_map.cdr");

    }

    // Update is called once per frame
    void Update () {
        if (c++ == 50)
        {
            CObject obj = Parser.processStreamObject(data);
        }
    }
}

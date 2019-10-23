using Assets;
using Assets.Database;
using Assets.RiftAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class dimtest : MonoBehaviour {

    string error;
    AssetDatabase adb;
    DB db;
    // Use this for initialization
    void Start () {
        DBInst.progress += (s) => this.error = s;
        DBInst.loadOrCallback((d) => db = d);
        error = "Loading asset database";
        adb = AssetDatabaseInst.DB;
       
    }
	
	// Update is called once per frame
	void Update () {
		if (db != null)
        {
            string worldName = "pd_shoreward_island";
            string worldCDR = worldName + "_map.cdr";
            Assets.GameWorld.worldName = worldName;
            Assets.WorldStuff.CDRParse.getMinMax(worldCDR, ref Assets.GameWorld.maxX, ref Assets.GameWorld.maxY);


            // Find the nif for the name
            string nifForName = findNifForName("Wood Wall Torch");

            ObjectPosition item = new ObjectPosition(nifForName, Vector3.zero, Quaternion.identity, Vector3.zero, 1);
            Assets.GameWorld.staticObjects.Add(item);


            SceneManager.LoadScene("scene1");
            db = null;
        }
	}

    private string findNifForName(string v)
    {
        return null;   
    }
}

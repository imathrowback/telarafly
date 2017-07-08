using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Threading;
using System;
using System.IO;

public class telara_obj : MonoBehaviour {

    public string file;
    public bool doLoad = false;
    public bool loaded = false;
    public Assets.RiftAssets.AssetDatabase.RequestCategory cat = Assets.RiftAssets.AssetDatabase.RequestCategory.NONE;
    telera_spawner spawner;

    public void setFile(String str)
    {
        file = str;
    }
    public void setProps(Assets.RiftAssets.AssetDatabase.RequestCategory cat, telera_spawner spawner)
    {
        this.spawner = spawner;
        this.cat = cat;
    }
    /*
    public void objectVisible()
    {
        if (doLoad || loaded)
            return;
        doLoad = true;
        startJob();
    }

    void startJob()
    {
        spawner.addJob(this, file);
    }
    */

    public void unload()
    {
        foreach (Transform child in transform)
           GameObject.DestroyImmediate(child.gameObject);
        doLoad = loaded = false;
    }

    void Start () {
    }

    
}

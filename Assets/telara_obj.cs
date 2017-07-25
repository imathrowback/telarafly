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

    public void setFile(String str)
    {
        file = str;
    }
    public void setProps(Assets.RiftAssets.AssetDatabase.RequestCategory cat)
    {
        this.cat = cat;
    }

    public void unload()
    {
        foreach (Transform child in transform)
           GameObject.DestroyImmediate(child.gameObject);
        doLoad = loaded = false;
    }

    void Start () {
    }

    
}

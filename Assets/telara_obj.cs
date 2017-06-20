using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Threading;
using System;
using System.IO;

public class telara_obj : MonoBehaviour {

    private string file;
    public bool doLoad = false;
    public bool loaded = false;
    GameObject mcamera;
    telera_spawner spawner;


    public void setFile(String str)
    {
        file = str;
    }
    void objectVisible()
    {
        if (doLoad || loaded)
            return;
        doLoad = true;
        startJob();
    }

    void startJob()
    {
        if (mcamera == null)
            mcamera = GameObject.Find("Main Camera");
        if (spawner == null)
            spawner = mcamera.GetComponent<telera_spawner>();

        spawner.addJob(this, file);
    }

    public void unload()
    {
        foreach (Transform child in transform)
           GameObject.DestroyImmediate(child.gameObject);
        doLoad = loaded = false;
    }

    void Start () {
        mcamera = GameObject.Find("Main Camera");
        spawner = mcamera.GetComponent<telera_spawner>();

    }

    
}

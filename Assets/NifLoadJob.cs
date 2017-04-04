using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NifLoadJob : ThreadedJob
{
    public volatile static int count = 0;

    //public Vector3[] InData;  // arbitary job data
    //public Vector3[] OutData; // arbitary job data
    public string filename;
    public GameObject go;
    public telara_obj parent;
    NIFLoader loader;
    public NifLoadJob(NIFLoader loader)
    {
        this.loader = loader;
    }
    protected override void ThreadFunction()
    {
        count++;
        // Do your threaded task. DON'T use the Unity API here
    }
    protected override void OnFinished()
    {
        count--;
        // This is executed by the Unity main thread when the job is finished
        go = loader.loadNIF(filename);
        go.transform.SetParent(parent.transform);
        go.transform.localScale = Vector3.one;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
    }
}

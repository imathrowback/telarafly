using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OBJLoadJob : ThreadedJob
{
    public volatile static int count = 0;

    //public Vector3[] InData;  // arbitary job data
    //public Vector3[] OutData; // arbitary job data
    public string filename;
    private OBJLoader.PreparedMesh prepMesh;
    public GameObject go;
    public telara_obj parent;
    bool useCache = false;
    protected override void ThreadFunction()
    {
        count++;
        // Do your threaded task. DON'T use the Unity API here
        if (OBJLoader.cacheExists(filename))
        {
            useCache = true;
            return;
        }
        prepMesh = OBJLoader.prepare(filename);
    }
    protected override void OnFinished()
    {
        count--;
        // This is executed by the Unity main thread when the job is finished
        if (useCache)
            go = OBJLoader.getCachedObject(filename);
        else go = OBJLoader.getGameObject(prepMesh, filename);
        go.transform.SetParent(parent.transform);
        //parent.transform.localScale = new Vector3(-1, 1, -1);
        go.transform.localScale = Vector3.one;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
    }
}

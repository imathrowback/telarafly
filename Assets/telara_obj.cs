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

#if UNITY_EDITOR1
    Camera cam;
    public float distanceTocam = 0;
    void Update()
    {
        if (cam == null)
            cam = GameObject.FindObjectOfType<Camera>();
        distanceTocam = Vector3.Distance(cam.transform.position, this.transform.position);
        if (distanceTocam > 100)
            return;
        Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
        if (TestPlanesAABB(camPlanes, this.transform.position))
        {
            Debug.DrawLine(this.transform.position, cam.transform.position, Color.green);
        }
        else
        {
            Debug.DrawLine(this.transform.position, cam.transform.position, Color.red);
        }

    }

    private bool TestPlanesAABB(Plane[] camPlanes, Vector3 point)
    {
        foreach (Plane p in camPlanes)
        {
            if (!p.GetSide(point))
                return false;
        }
        return true;
    }
#endif
}

using System;
using System.Collections.Generic;
using UnityEngine;

using Assets.NIF;
using System.Threading;

public class NifLoadJob : ThreadedJob
{
    public volatile static int count = 0;
    public Guid uid = Guid.NewGuid();
    static Dictionary<String, GameObject> originals = new Dictionary<string, GameObject>();
    static Dictionary<String, Semaphore> cacheWait = new Dictionary<string, Semaphore>();

    public static GameObject getCachedObject(string fn)
    {
        lock (originals)
        {
            if (originals.ContainsKey(fn))
            {
                GameObject go = originals[fn];
                // check if the object has been destroyed before we try to use it
                if (go != null)
                {
                    GameObject newG = GameObject.Instantiate(go);
                    return newG;
                }
                // object was destroyed, return null;
                originals.Remove(fn);
                return null;
            }
            return null;
        }
    }

    public static void clearCache()
    {
        originals.Clear();
        cacheWait.Clear();
    }


    //public Vector3[] InData;  // arbitary job data
    //public Vector3[] OutData; // arbitary job data
    public string filename;

    public telara_obj parent;
    NIFFile niffile;
    NIFFile lodfile;


    public NifLoadJob( string file)
    {
        this.filename = file;
        lock (cacheWait)
        {
            if (!cacheWait.ContainsKey(filename))
                cacheWait[filename] = new Semaphore(1, 1);
        }
    }
    protected override void ThreadFunctionCDR()
    {
        // Do your threaded task. DON'T use the Unity API here

        count++;

        cacheWait[filename].WaitOne();

        lock (originals)
        {
            // if our cache contains an object, return it immediately
            if (originals.ContainsKey(filename))
                return;
        }
        try
        {
            niffile = NIFLoader.getNIF(filename, parent.cat);

            // extra check for terrain
            if (filename.Contains("_terrain_"))
            {
                string lodname = filename.Replace("_split", "_lod_split");
                try
                {
                    lodfile = NIFLoader.getNIF(lodname, parent.cat);
                }
                catch (Exception ex)
                {
                    Debug.Log("there was an exception while trying to load lod split:" + lodname + ": " + ex);
                }
            }


        }
        catch (Exception ex)
        {
            Debug.Log("there was an exception while doing the thread:" + filename + ": " + ex);
        }
    }
    protected override void OnFinished()
    {
        try
        {
            if (filename.Contains("_terrain_"))
                parent.gameObject.AddComponent<TerrainObj>();
            GameObject go;
            count--;
            // This is executed by the Unity main thread when the job is finished
            if (niffile != null)
            {
                go = NIFLoader.loadNIF(niffile, filename);
                lock (originals)
                {
                    originals[filename] = go;
                }
            }
            else
                go = getCachedObject(filename);

            if (go != null)
            {
                if (Assets.GameWorld.useColliders)
                {
                    GameObject.Destroy(parent.GetComponent<BoxCollider>());
                    GameObject.Destroy(parent.GetComponent<SphereCollider>());
                }
                go.transform.SetParent(parent.transform);
                go.transform.localScale = Vector3.one;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Unable to load nif:" + niffile + " " + filename);
            Debug.Log(ex);
        }
        finally
        {
            cacheWait[filename].Release();
        }
    }
}

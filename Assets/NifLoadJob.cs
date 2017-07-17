using System;
using System.Collections.Generic;
using UnityEngine;

using Assets.NIF;
using System.Threading;

public class NifLoadJob : ThreadedJob
{
    public volatile static int count = 0;
    public static long guid = 0;
    public long uid = guid++;
    //public Guid uid = Guid.NewGuid();
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

    public Vector3 parentPos { get; internal set; }

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
            //Debug.Log("there was an exception while doing the thread:" + filename + ": " + ex);
        }
    }
    protected override void OnFinished()
    {
        GameObject go = null;
        try
        {
            if (filename.Contains("_terrain_"))
                parent.gameObject.AddComponent<TerrainObj>();
            count--;
            // This is executed by the Unity main thread when the job is finished
            if (niffile != null)
            {
                go = NIFLoader.loadNIF(niffile, filename);
                if (lodfile != null)
                {
                    GameObject lodgo = NIFLoader.loadNIF(lodfile, filename);

                    // terrain lod
                    LODGroup group = go.GetComponent<LODGroup>();
                    if (group == null)
                        group = go.AddComponent<LODGroup>();
                    group.animateCrossFading = true;
                    group.fadeMode = LODFadeMode.SpeedTree;
                    LOD[] lods = new LOD[2];
                    Renderer[] renderersMax = go.GetComponentsInChildren<Renderer>();
                    Renderer[] renderersLow = lodgo.GetComponentsInChildren<Renderer>();
                    lods[0] = new LOD(0.6f, renderersMax);
                    lods[1] = new LOD(0.03f, renderersLow);
                    //lods[1] = new LOD(1f - LODCutoff, renderers);
                    group.SetLODs(lods);

                    GameObject lodObj = new GameObject();
                    lodObj.name = "LOD-" + filename;
                    lodgo.transform.SetParent(lodObj.transform);
                    lodObj.transform.SetParent(go.transform);

                }

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
            if (null != go)
                GameObject.Destroy(go);
        }
        finally
        {
            cacheWait[filename].Release();
        }
    }
}

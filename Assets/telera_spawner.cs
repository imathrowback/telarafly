using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using Assets;

public class telera_spawner : MonoBehaviour
{
    GameObject meshRoot;
    Properties p;
    Queue<string> nodeBuildQueue = new Queue<string>();
    List<NifLoadJob> ObjJobLoadQueue = new List<NifLoadJob>();
    GameObject mcamera;
    camera_movement camMove;
    System.IO.StreamReader fileStream;
    
    int MAX_RUNNING_THREADS = 4;
    int MAX_OBJ_PER_FRAME = 150;
    int MAX_NODE_PER_FRAME = 15025;
    NIFLoader nifloader;

    public void addJob(telara_obj parent, string filename)
    {
        NifLoadJob job = new NifLoadJob(nifloader, filename);
        job.parent = parent;
        addJob(job);
    }
    public void addJob(NifLoadJob job)
    {
        // prioritize terrain, otherwise add the job to the end of the list
        if (job.filename.Contains("terrain") || job.filename.Contains("ocean_chunk"))
            ObjJobLoadQueue.Insert(0, job);
        else
            ObjJobLoadQueue.Add(job);
    }
    public int getNodeBuildSize()
    {
        return nodeBuildQueue.Count;
    }
    public int ObjJobLoadQueueSize()
    {
        return ObjJobLoadQueue.Count;
    }
    // Use this for initialization
    void Start()
    {
        mcamera = GameObject.Find("Main Camera");
        meshRoot = GameObject.Find("ROOT");
        camMove = mcamera.GetComponent<camera_movement>();
        // clear all the children of the root before we re-add them
        //foreach (Transform child in meshRoot.transform)
        // {
        //     GameObject.Destroy(child.gameObject);
        //}

        p = new Properties("nif2obj.properties");
        MAX_OBJ_PER_FRAME = int.Parse(p.get("MAX_OBJ_PER_FRAME", "100"));
        MAX_RUNNING_THREADS = int.Parse(p.get("MAX_RUNNING_THREADS", "4"));
        MAX_NODE_PER_FRAME = int.Parse(p.get("MAX_NODE_PER_FRAME", "15000"));
       
        this.transform.localPosition = GameWorld.initialSpawn.pos;
        Vector3 angles = this.transform.localEulerAngles;
        angles.x = 0;
        angles.z = 0;
        angles.y = Mathf.Rad2Deg * -GameWorld.initialSpawn.angle;
        this.transform.localEulerAngles = angles;

        Debug.Log("begin loading database");
        this.nifloader = new NIFLoader();
        this.nifloader.loadManifestAndDB();
        // load the ref db

        /*
                string file = p.get("WORLD");
                fileStream = new System.IO.StreamReader(file);

                string[] lines = File.ReadAllLines(file);
                int i = 0;
                foreach (string line in lines)
                {
                    nodeBuildQueue.Enqueue(line);
                }
                */
        Debug.Log("loading " + GameWorld.getObjects().Count + " objects");
        foreach (ObjectPosition op in GameWorld.getObjects())
        {
            process(op);
        }

    }

    private void processNodeLine(string line)
    {
        string[] parts = line.Split(':');
        string name = parts[0];
        string poss = parts[1];
        string rots = parts[2];
        string pos2 = parts[3];
        float scale = 1.0f;
        if (parts.Length == 5)
            scale = float.Parse(parts[4]);

        if (name.Trim().Length == 0)
        {
            return;
        }

        Vector3 pos = getV3(poss);
        Vector3 poss2 = getV3(pos2);
        Vector4 q = getV4(rots);
        process(new ObjectPosition(name, pos, new Quaternion(q.x, q.y, q.z, q.w), poss2, scale));
    }

    private void process(ObjectPosition op)
    {
        GameObject go;
        string name = op.nifFile;
        if (name.Contains("_terrain_") || name.Contains("ocean_chunk"))
        {
            // use a vertical box as our collider for now
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            float c = float.Parse(p.get("TERRAIN_VIS", "15"));
            go.GetComponent<BoxCollider>().size = new Vector3(256 * c, 5000, 256 * c);
            go.GetComponent<BoxCollider>().center = new Vector3(128, 0, 128);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.GetComponent<SphereCollider>().radius = 5;
        }
        telara_obj tobj = go.AddComponent<telara_obj>();
        go.transform.SetParent(meshRoot.transform);
        GameObject.Destroy(go.GetComponent<MeshRenderer>());

        tobj.setFile(name);
        go.name = name;
        go.transform.localScale = new Vector3(op.scale, op.scale, op.scale);
        go.transform.localPosition = op.min;
        go.transform.localRotation = op.qut;
    }

    Vector3 getV3(string str)
    {
        string fstr = str.Replace("(", "").Replace(")", "");
        string[] parts = fstr.Split(',');
        return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
    }
    Vector4 getV4(string str)
    {
        string fstr = str.Replace("(", "").Replace(")", "");
        string[] parts = fstr.Split(',');
        return new Vector4(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
    }

    int runningThreads = 0;
    bool nodeBuildQueueDone = false;

    // Update is called once per frame
    void Update()
    {
        if (camMove.isRotating)
            return;

        int i = 0;
        while (nodeBuildQueue.Count > 0 && i++ < MAX_NODE_PER_FRAME)
            processNodeLine(nodeBuildQueue.Dequeue());
        if (i > 1)
        {
            if (nodeBuildQueue.Count == 0)
                camera_movement.checkHits(mcamera.transform.position);
            //            return;
        }

        i = 0;


        foreach (NifLoadJob job in ObjJobLoadQueue.ToArray())
        {
            if (i > MAX_OBJ_PER_FRAME)
            {
                Debug.Log("spawned " + i + " this frame, thats enough for now");
                break;
            }

            telara_obj obj = job.parent;
            if (obj.gameObject.transform.childCount == 0)
            {
                GameObject loading = (GameObject)GameObject.Instantiate(Resources.Load("LoadingCapsule"));
                loading.name = "Loading";
                loading.transform.parent = obj.gameObject.transform;
                loading.transform.localPosition = Vector3.zero;
                loading.transform.localRotation = Quaternion.identity;
            }


            if (runningThreads < MAX_RUNNING_THREADS && !job.IsStarted)
            {
                job.Start(System.Threading.ThreadPriority.Lowest);
                runningThreads++;
            }
            if (job.Update())
            {
                telara_obj to = job.parent;
                Transform loadingObj = to.gameObject.transform.FindChild("Loading");
                if (loadingObj != null)
                    GameObject.Destroy(loadingObj.gameObject);


                to.doLoad = false;
                to.loaded = true;
                ObjJobLoadQueue.Remove(job);
                runningThreads--;
                i++;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using Assets;
using UnityStandardAssets.Characters.ThirdPerson;
using UnityEngine.UI;

public class telera_spawner : MonoBehaviour
{
    GameObject meshRoot;
    Properties p;
    Queue<string> nodeBuildQueue = new Queue<string>();
    List<NifLoadJob> ObjJobLoadQueue = new List<NifLoadJob>();
    GameObject charC;
    ThirdPersonUserControl tpuc;
    Rigidbody tpucRB;
    GameObject mcamera;
    //camera_movement camMove;
    System.IO.StreamReader fileStream;

    int MAX_RUNNING_THREADS = 4;
    int MAX_OBJ_PER_FRAME = 150;
    int MAX_NODE_PER_FRAME = 15025;
    NIFLoader nifloader;

    bool firstLoad = true;
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
        charC = GameObject.Find("ThirdPersonController");
        if (charC != null)
        {
            tpuc = charC.GetComponent<ThirdPersonUserControl>();
            tpucRB = charC.GetComponent<Rigidbody>();
            charC.SetActive(false);
        }
        dropdown = GameObject.Find("SpawnDropdown").GetComponent<Dropdown>();
        mcamera = GameObject.Find("Main Camera");
        meshRoot = GameObject.Find("NIFRotationRoot");

        p = new Properties("nif2obj.properties");
        MAX_OBJ_PER_FRAME = int.Parse(p.get("MAX_OBJ_PER_FRAME", "100"));
        MAX_RUNNING_THREADS = int.Parse(p.get("MAX_RUNNING_THREADS", "4"));
        MAX_NODE_PER_FRAME = int.Parse(p.get("MAX_NODE_PER_FRAME", "15000"));

        setCameraLoc(GameWorld.initialSpawn);

        dropdown.gameObject.SetActive(false);
        dropdown.options.Clear();
        int startIndex = 0;
        int i = 0;
        foreach (WorldSpawn spawn in GameWorld.getSpawns())
        {
            if (spawn.spawnName.Equals(GameWorld.initialSpawn.spawnName))
                startIndex = i;
            Dropdown.OptionData option = new Dropdown.OptionData(spawn.worldName + " - " + spawn.spawnName + " - " + spawn.pos);
            dropdown.options.Add(option);
            i++;
        }
        dropdown.value = startIndex;
        dropdown.gameObject.SetActive(true);
        dropdown.RefreshShownValue();

        Debug.Log("begin loading database");
        this.nifloader = new NIFLoader();
        this.nifloader.loadManifestAndDB();


        Debug.Log("loading " + GameWorld.getObjects().Count + " objects");
        foreach (ObjectPosition op in GameWorld.getObjects())
        {
            process(op);
        }

    }

    public void setCameraLoc(WorldSpawn spawn, bool useChar = false)
    {
        if (charC != null && useChar)
        {
            GameObject.Destroy(GetComponent<cam.camera_movement>());

            mcamera.transform.parent = charC.transform;
            charC.SetActive(true);

            Transform charCTransform = charC.transform;
            Transform charCTParent = charCTransform.parent;
            charCTransform.parent = meshRoot.transform;
            charCTransform.transform.localPosition = spawn.pos;
            charCTransform.parent = charCTParent;
            charC.transform.localEulerAngles = new Vector3(0, Mathf.Rad2Deg * spawn.angle, 0);

            mcamera.transform.localEulerAngles = new Vector3(19, 0, 0);
            mcamera.transform.localPosition = new Vector3(0, 2.6f, -4);
            GameObject.Destroy(mcamera.gameObject.GetComponent<cam.camera_movement>());
        }
        else
        if (spawn != null)
        {
            Transform camTransform = mcamera.transform;
            Transform camTParent = camTransform.parent;

            camTransform.parent = meshRoot.transform;
            camTransform.transform.localPosition = spawn.pos;
            camTransform.parent = camTParent;

            mcamera.transform.localEulerAngles = new Vector3(0, Mathf.Rad2Deg * -spawn.angle, 0);
        }
    }

    public void dropdownChange()
    {
        GameWorld.initialSpawn = GameWorld.getSpawns()[dropdown.value];
        setCameraLoc(GameWorld.initialSpawn);
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
            go.layer = 30;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.GetComponent<SphereCollider>().radius = 5;
            go.layer = 30;
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
    private Dropdown dropdown;

    public Collider[] checkHits(Vector3 position)
    {
        //Debug.Log("camera moved, update colliders");
        Collider[] hitColliders = Physics.OverlapSphere(position, float.Parse(p.get("OBJECT_VISIBLE", "500")), 1 << 30);

        System.Array.Sort(hitColliders, (b, a) => Vector3.Distance(position, a.gameObject.transform.position).CompareTo(Vector3.Distance(position, b.gameObject.transform.position)));


        int i = 0;
        while (i < hitColliders.Length)
        {
            Collider c = hitColliders[i++];
            c.SendMessage("objectVisible", SendMessageOptions.DontRequireReceiver);
        }
        return hitColliders;
    }


    // Update is called once per frame
    void Update()
    {
        checkHits(this.mcamera.transform.position);
        if (tpuc != null && tpuc.isRotating)
            return;
        /*
        if (Input.GetKeyDown(KeyCode.O))
        {
            GameObject go = new GameObject();
            go.transform.parent = meshRoot.transform;
            go.transform.localPosition = this.mcamera.transform.localPosition;
            go.transform.localRotation = this.mcamera.transform.localRotation;
            go.AddComponent<ModelView>();
        }
        */
        if (Input.GetKeyDown(KeyCode.P) && GameWorld.useColliders && charC != null)
        {
            setCameraLoc(GameWorld.initialSpawn, true);
        }
        int i = 0;
        while (nodeBuildQueue.Count > 0 && i++ < MAX_NODE_PER_FRAME)
            processNodeLine(nodeBuildQueue.Dequeue());
        if (i > 1)
        {
            if (nodeBuildQueue.Count == 0)
                checkHits(charC.transform.position);
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

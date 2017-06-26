using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Threading;
using Assets;
using UnityStandardAssets.Characters.ThirdPerson;
using UnityEngine.UI;
using Assets.Database;
using Assets.Wardrobe;
using Assets.WorldStuff;

public class telera_spawner : MonoBehaviour
{
    GameObject meshRoot;
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

    public void purgeObjects()
    {
        if (ObjJobLoadQueue.Count > 0)
            return;
        NifLoadJob.clearCache();
        foreach (telara_obj obj in GameObject.FindObjectsOfType<telara_obj>())
        {
            // don't unload terrain
            if (obj.gameObject.GetComponent<TerrainObj>() == null)
                obj.unload();
        }
        // clear the job queue as well
        NifLoadJob[] queue = ObjJobLoadQueue.ToArray();
        ObjJobLoadQueue.Clear();
        foreach (NifLoadJob job in queue)
        {
            telara_obj obj = job.parent;
            if (obj.gameObject.GetComponent<TerrainObj>() != null)
                ObjJobLoadQueue.Add(job);
        }
    }

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

        MAX_OBJ_PER_FRAME = ProgramSettings.get("MAX_OBJ_PER_FRAME", 100);
        MAX_RUNNING_THREADS = ProgramSettings.get("MAX_RUNNING_THREADS", 4);
        MAX_NODE_PER_FRAME = ProgramSettings.get("MAX_NODE_PER_FRAME", 15000);

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

        /*
        Debug.Log("loading " + GameWorld.getObjects().Count + " objects");
        foreach (ObjectPosition op in GameWorld.getObjects())
        {
            process(op);
        }
        */

    }

    HashSet<string> processedTiles = new HashSet<string>();

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

    private void process(ObjectPosition op)
    {
        GameObject go;
        if (op is LightPosition)
        {
            LightPosition lp = (LightPosition)op;
            go = new GameObject("Light");
            go.transform.SetParent(meshRoot.transform);
            go.transform.localScale = new Vector3(op.scale, op.scale, op.scale);
            go.transform.localPosition = op.min;
            go.transform.localRotation = op.qut;

            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(lp.r, lp.g, lp.b);
            light.intensity = lp.range;
            light.shadows = LightShadows.Hard;
            return;
        }

        string name = op.nifFile;
        Assets.RiftAssets.AssetDatabase.RequestCategory category = Assets.RiftAssets.AssetDatabase.RequestCategory.NONE;
        if (name.Contains("_terrain_") || name.Contains("ocean_chunk"))
        {
            // use a vertical box as our collider for now
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            float c = ProgramSettings.get("TERRAIN_VIS", 15.0f);
            go.GetComponent<BoxCollider>().size = new Vector3(256 * c, 5000, 256 * c);
            go.GetComponent<BoxCollider>().center = new Vector3(128, 0, 128);
            go.layer = 30;
            if (name.Contains("_terrain_"))
                category = Assets.RiftAssets.AssetDatabase.RequestCategory.GEOMETRY;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Bounds b = new Bounds(op.min, Vector3.one);
            b.Encapsulate(op.max);
            
            
            go.GetComponent<SphereCollider>().radius = b.size.magnitude;
            go.layer = 30;
        }
        telara_obj tobj = go.AddComponent<telara_obj>();
        tobj.setProps(this, mcamera, category);

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
    private Dropdown dropdown;

    public Collider[] checkHits(Vector3 position)
    {
        //Debug.Log("camera moved, update colliders");
        Collider[] hitColliders = Physics.OverlapSphere(position, ProgramSettings.get("OBJECT_VISIBLE", 500), 1 << 30);

        System.Array.Sort(hitColliders, (b, a) => Vector3.Distance(position, a.gameObject.transform.position).CompareTo(Vector3.Distance(position, b.gameObject.transform.position)));


        int i = 0;
        while (i < hitColliders.Length)
        {
            Collider c = hitColliders[i++];
            c.SendMessage("objectVisible", SendMessageOptions.DontRequireReceiver);
        }
        return hitColliders;
    }

    GameObject mount;


    private Vector3 getWorldCamPos()
    {
        return meshRoot.transform.InverseTransformPoint(mcamera.transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        checkHits(this.mcamera.transform.position);
        if (tpuc != null && tpuc.isRotating)
            return;

        if (Input.GetKeyDown(KeyCode.F) && mount == null)
        {
            mount = AnimatedModelLoader.loadNIF(1445235995);
            AnimatedNif animNif = mount.GetComponent<AnimatedNif>();
            animNif.animSpeed = 0.02f;
            animNif.setSkeletonRoot(mount);
            animNif.setActiveAnimation("mount_dragon_jump_cycle");
            mount.transform.parent = mcamera.transform;
            mount.transform.localRotation = Quaternion.identity;
            mount.transform.localPosition = new Vector3(0, -5.91f, 7.66f);
            // human_female_mount_dragon_jump_cycle.kf

            GameObject character = new GameObject();
            
            Paperdoll mainPaperdoll = character.AddComponent<Paperdoll>();
            mainPaperdoll.animOverride = "mount_dragon_jump_cycle";
            mainPaperdoll.kfbOverride = "human_female_mount.kfb";
            mainPaperdoll.setGender("female");
            mainPaperdoll.setRace("human");
            //mainPaperdoll.GetComponent<AnimatedNif>().animSpeed = 0.02f;
            mainPaperdoll.animSpeed = 0.02f;
            character.transform.parent = mount.transform;
            character.transform.localPosition = new Vector3(0, 0, 0);
            character.transform.localRotation = Quaternion.identity;
            mainPaperdoll.transform.localRotation = Quaternion.identity;

            mainPaperdoll.updateRaceGender();
            mainPaperdoll.loadAppearenceSet(-57952362);


        }

        if (Input.GetKeyDown(KeyCode.P) && GameWorld.useColliders && charC != null)
        {
            setCameraLoc(GameWorld.initialSpawn, true);
        }
        int i = 0;

        Vector3 camPos = mcamera.transform.position;
        IOrderedEnumerable<NifLoadJob> processQueue = ObjJobLoadQueue.OrderBy(a => !(a.filename.Contains("terrain") || a.filename.Contains("ocean"))).ThenBy(a => Vector3.Distance(a.parent.transform.position, camPos));

        int tileX = Mathf.FloorToInt(getWorldCamPos().x / 256.0f) ;
        int tileY = Mathf.FloorToInt(getWorldCamPos().z / 256.0f) ;
        int[][] v = {
            new int[]{ -1, 1 },  new int[]{ 0, 1 },   new int[]{ 1, 1 },
            new int[]{ -1, 0 },  new int[]{ 0, 0 },   new int[]{ 1, 0 },
            new int[]{ -1, -1 },  new int[]{ 0, -1 },   new int[]{ 1, -1 },
        };
        int range = 5;
        for (int tx = tileX - range; tx < tileX + range; tx++)
        {
            for (int ty = tileY - range; ty < tileY + range; ty++)
            {
                string tileStr = tx + ":" + ty;
                if (!processedTiles.Contains(tileStr))
                {
                    CDRParse.doWorldTile(AssetDatabaseInst.DB, DBInst.inst, GameWorld.worldName, tx * 256, ty * 256, (p) => process(p));
                    processedTiles.Add(tileStr);
                }
            }
        }


        foreach (NifLoadJob job in processQueue)
        {

            if (i > MAX_OBJ_PER_FRAME)
            {
                Debug.Log("spawned " + i + " this frame, thats enough for now");
                break;
            }

            /** Add a loading capsule to the location of the job */
            telara_obj obj = job.parent;
            if (obj.gameObject.transform.childCount == 0)
            {
                GameObject loading = (GameObject)GameObject.Instantiate(Resources.Load("LoadingCapsule"));
                loading.name = "Loading";
                SphereCollider sp = obj.GetComponent<SphereCollider>();
                if (sp != null)
                    loading.transform.localScale = Vector3.one * sp.radius;
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
                Transform loadingObj = to.gameObject.transform.Find("Loading");
                if (loadingObj != null)
                    GameObject.Destroy(loadingObj.gameObject);
                if (to.gameObject != null)
                {
                    // reapply the lod to take into account any new meshes created
                    applyLOD(to.gameObject);
                }
                runningThreads--;
                i++;
                to.doLoad = false;
                to.loaded = true;
                ObjJobLoadQueue.Remove(job);
            }
        }
    }

    private void applyLOD(GameObject go)
    {
        LODGroup group = go.GetComponent<LODGroup>();
        if (group == null)
            group = go.AddComponent<LODGroup>();

        group.animateCrossFading = true;
        group.fadeMode = LODFadeMode.SpeedTree;
        LOD[] lods = new LOD[2];
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        lods[0] = new LOD(0.9f, renderers);
        lods[1] = new LOD(0.1f, renderers);
        group.SetLODs(lods);


    }
}

using SCG = System.Collections.Generic;
using KdTree;
using C5;
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
using System;

public class telera_spawner : MonoBehaviour
{
    GameObject meshRoot;
    SCG.List<ObjectPosition> objectPositions;
//    TreeDictionary<Guid, NifLoadJob> objsToCreateList;
    GameObject charC;
    ThirdPersonUserControl tpuc;
    Rigidbody tpucRB;
    public GameObject mcamera;
    //camera_movement camMove;
    System.IO.StreamReader fileStream;

    int MAX_RUNNING_THREADS = 6;
    int MAX_NODE_PER_FRAME = 15025;

    public void purgeObjects()
    {
        
        NifLoadJob.clearCache();
        foreach (telara_obj obj in GameObject.FindObjectsOfType<telara_obj>())
        {
            // don't unload terrain
            if (obj.gameObject.GetComponent<TerrainObj>() == null)
                obj.unload();
        }
       
    }


    public void addJob(telara_obj parent, string filename)
    {
        NifLoadJob job = new NifLoadJob( filename);
        job.parent = parent;
        Vector3 pos = parent.transform.position;
        float[] floatf = new float[] { pos.x, pos.z };
        SCG.List<NifLoadJob> nList;
        if (job.filename.Contains("terrain") || job.filename.Contains("ocean"))
        {
            if (!this.terraintree.TryFindValueAt(floatf, out nList))
            {
                nList = new SCG.List<NifLoadJob>();
                nList.Add(job);
                this.terraintree.Add(floatf, nList);
            }
            else
                nList.Add(job);
        }
        else
        {
            if (!this.postree.TryFindValueAt(floatf, out nList))
            {
                nList = new SCG.List<NifLoadJob>();
                nList.Add(job);
                this.postree.Add(floatf, nList);
            }
            else
                nList.Add(job);
        }
    }

    public int ObjJobLoadQueueSize()
    {
        return candidateMeshesToLoad + runningList.Count();
    }

    class NifLoadJobCompare : SCG.IComparer<NifLoadJob>
    {
        public int Compare(NifLoadJob x, NifLoadJob y)
        {
            if (x.filename.Contains("terrain") || x.filename.Contains("ocean"))
                return -1;
            return 0;
        }
    }

    class NifLoadJobCompareDist : SCG.IComparer<NifLoadJob>
    {
        Vector3 v;
        public NifLoadJobCompareDist(Vector3 v)
        {
            this.v = v;
        }
        public int Compare(NifLoadJob x, NifLoadJob y)
        {
            Vector3 a = x.parent.transform.position;
            Vector3 b = x.parent.transform.position;
            float distA = Vector3.Distance(a, v);
            float distB = Vector3.Distance(a, v);
            if (distA < distB)
                return -1;
            if (distB < distA)
                return 1;
            return 0;
        }
    }

    class ObjectPositionCompare : SCG.IComparer<ObjectPosition>
    {
        public int Compare(ObjectPosition x, ObjectPosition y)
        {
            if (x.nifFile.Contains("terrain") || x.nifFile.Contains("ocean"))
                return -1;
            return 0;
        }
    }

    // Use this for initialization
    void Start()
    {
        Slider lodslider = GameObject.Find("LODSlider").GetComponent<Slider>();
        this.LODCutoff = PlayerPrefs.GetFloat("worldLodSlider", 0.9f);
        lodslider.value = this.LODCutoff;



        objectPositions = new SCG.List<ObjectPosition>();

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
           DOption option = new DOption(spawn.worldName + " - " + spawn.spawnName + " - " + spawn.pos, false);
            dropdown.options.Add(option);
            i++;
        }
        dropdown.value = startIndex;
        dropdown.gameObject.SetActive(true);
        dropdown.GetComponent<FavDropDown>().doOptions();
        dropdown.RefreshShownValue();

        Debug.Log("begin loading database");
        

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

    private GameObject process(ObjectPosition op)
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
            return go;
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
        tobj.setProps(category, this);

        go.transform.SetParent(meshRoot.transform);
        GameObject.Destroy(go.GetComponent<MeshRenderer>());

        tobj.setFile(name);
        go.name = name;
        go.transform.localScale = new Vector3(op.scale, op.scale, op.scale);
        go.transform.localPosition = op.min;
        go.transform.localRotation = op.qut;

        
        return go;
    }

    private void addLoading(NifLoadJob job)
    {
        // Add a loading capsule to the location of the job 
        //if (job.parent.gameObject.transform.childCount == 0)
        {
            telara_obj obj = job.parent;
            GameObject loading = (GameObject)GameObject.Instantiate(Resources.Load("LoadingCapsule"));
            loading.name = "Loading";
            SphereCollider sp = obj.GetComponent<SphereCollider>();
            if (sp != null)
                loading.transform.localScale = Vector3.one*3;
            loading.transform.parent = obj.gameObject.transform;
            loading.transform.localPosition = Vector3.zero;
            loading.transform.localRotation = Quaternion.identity;
            //applyLOD(loading);
        }

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


    KdTree.KdTree<float, SCG.List<NifLoadJob>> postree = new KdTree<float, SCG.List<NifLoadJob>>(2, new KdTree.Math.FloatMath(), AddDuplicateBehavior.Error);
    KdTree.KdTree<float, SCG.List<NifLoadJob>> terraintree = new KdTree<float, SCG.List<NifLoadJob>>(2, new KdTree.Math.FloatMath(), AddDuplicateBehavior.Error);
    int candidateMeshesToLoad = 0;

    public bool IsVisibleFrom(Vector3 v, Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, new Bounds(v, Vector3.one));
    }


    TreeDictionary<Guid, NifLoadJob> runningList;
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

            mainPaperdoll.setAppearenceSet(-57952362);
        }

        if (Input.GetKeyDown(KeyCode.P) && GameWorld.useColliders && charC != null)
        {
            setCameraLoc(GameWorld.initialSpawn, true);
        }
       

        /**
         * Load the world tiles around the camera and their objects 
         */
        int tileX = Mathf.FloorToInt(getWorldCamPos().x / 256.0f) ;
        int tileY = Mathf.FloorToInt(getWorldCamPos().z / 256.0f) ;
        int[][] v = {
            new int[]{ -1, 1 },  new int[]{ 0, 1 },   new int[]{ 1, 1 },
            new int[]{ -1, 0 },  new int[]{ 0, 0 },   new int[]{ 1, 0 },
            new int[]{ -1, -1 },  new int[]{ 0, -1 },   new int[]{ 1, -1 },
        };
        int range = ProgramSettings.get("TERRAIN_VIS", 10);
        for (int txx = tileX - range; txx <= tileX + range; txx++)
        {
            for (int txy = tileY - range; txy <= tileY + range; txy++)
            {
                int tx = txx;
                int ty = txy;
                string tileStr = tx + ":" + ty;
                if (!processedTiles.Contains(tileStr))
                {
                    System.Threading.Thread m_Thread = new System.Threading.Thread(() =>
                    {
                        SCG.List<ObjectPosition> objs = new SCG.List<ObjectPosition>();
                        CDRParse.doWorldTile(AssetDatabaseInst.DB, DBInst.inst, GameWorld.worldName, tx * 256, ty * 256, (p) =>
                        {
                            objs.Add(p);
                        });
                        lock(objectPositions)
                        {
                            objectPositions.AddRange(objs);
                        }
                        
                    });
                    //m_Thread.Priority = System.Threading.ThreadPriority.Lowest;
                    m_Thread.Priority = (System.Threading.ThreadPriority)ProgramSettings.get("MAP_LOAD_THREAD_PRIORITY",(int)System.Threading.ThreadPriority.Normal);
                    m_Thread.Start();
                    processedTiles.Add(tileStr);
                }
            }
        }

        lock (objectPositions)
        {
            // don't spend more than 100ms in here
            DateTime end = DateTime.Now.AddMilliseconds(100);
            while (objectPositions.Count() > 0 && DateTime.Now < end)
            {
                ObjectPosition p = objectPositions[0];
                objectPositions.RemoveAt(0);
                GameObject go = process(p);
            }
        }

        Vector3 camPos = this.mcamera.transform.position;
        checkHits(camPos);

        if (runningList == null)
            runningList = new TreeDictionary<Guid, NifLoadJob>();

        foreach (NifLoadJob job in runningList.Values.ToArray())
        {
            if (job.Update())
            {
                finalizeJob(job);
                runningList.Remove(job.uid);
                //postree.RemoveAt()
            }
        }

        if (runningList.Count < MAX_RUNNING_THREADS)
        {
            float[] camPosF = new float[] { camPos.x, camPos.z };
            KdTreeNode<float, SCG.List<NifLoadJob>>[] tercandidates =// this.terraintree.RadialSearch(camPosF, ProgramSettings.get("OBJECT_VISIBLE", 500), MAX_RUNNING_THREADS);
                this.terraintree.GetNearestNeighbours(camPosF, MAX_RUNNING_THREADS);
            KdTreeNode<float, SCG.List<NifLoadJob>>[] candidates = this.postree.RadialSearch(camPosF, ProgramSettings.get("OBJECT_VISIBLE", 500), MAX_RUNNING_THREADS);
            //postree.GetNearestNeighbours(camPosF, MAX_RUNNING_THREADS);

            SCG.IEnumerable<NifLoadJob> terjobs = tercandidates.SelectMany(e => e.Value);
            SCG.IEnumerable<NifLoadJob> otherjobs = candidates.SelectMany(e => e.Value);
            // objects in camera view should be visible first
            Camera cam = mcamera.GetComponent<Camera>();
            terjobs = terjobs.OrderBy(n => !IsVisibleFrom(n.parent.transform.position, cam));
            otherjobs = otherjobs.OrderBy(n => !IsVisibleFrom(n.parent.transform.position, cam));

            SCG.List<NifLoadJob> jobs = new SCG.List<NifLoadJob>();
            if (terjobs.Count() > 0)
                jobs.Add(terjobs.First());
            jobs.AddRange(otherjobs);

            candidateMeshesToLoad = jobs.Count();

            // take items from the KDTree and move them to a render queue
            foreach (NifLoadJob job in jobs.ToArray())
            {
                if (runningList.Count < MAX_RUNNING_THREADS)
                {
                    job.Start((System.Threading.ThreadPriority)ProgramSettings.get("OBJECT_LOAD_THREAD_PRIORITY", (int)System.Threading.ThreadPriority.Normal));
                    runningList.Add(job.uid, job);
                    addLoading(job);


                    Vector3 pos = job.parent.transform.position;
                    float[] floatf = new float[] { pos.x, pos.z };

                    foreach (KdTreeNode<float, SCG.List<NifLoadJob>> n in tercandidates)
                        n.Value.Remove(job);
                    foreach (KdTreeNode<float, SCG.List<NifLoadJob>> n in candidates)
                        n.Value.Remove(job);
                }
                // we already reached max threads, break out
                else
                    break;
            }
            // cleanup empty kdtree nodes
            foreach (KdTreeNode<float, SCG.List<NifLoadJob>> n in tercandidates)
                if (n.Value.Count == 0)
                    terraintree.RemoveAt(n.Point);
            foreach (KdTreeNode<float, SCG.List<NifLoadJob>> n in candidates)
                if (n.Value.Count == 0)
                    postree.RemoveAt(n.Point);

        }
    }

    private bool finalizeJob(NifLoadJob job)
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

        to.doLoad = false;
        to.loaded = true;
        return true;
    }

    [SerializeField]
    bool useLOD = true;
    [SerializeField]
    float LODCutoff = 0.9f;

    /// <summary>
    /// Update the LOD on all objects
    /// </summary>
    public void updateLOD(bool newLod)
    {
        this.useLOD = newLod;
        telara_obj[] objs = GameObject.FindObjectsOfType<telara_obj>();
        foreach (telara_obj obj in objs)
        {
            if (!useLOD)
            {
                LODGroup group = obj.gameObject.GetComponent<LODGroup>();
                if (group != null)
                    GameObject.Destroy(group);
            }
            else
            {
                applyLOD(obj.gameObject);
            }
        }
    }

    GameObject lodObj;
    public void lodSliderChange()
    {
        if (lodObj == null)
            lodObj = GameObject.Find("LODSlider");
        Slider lodslider = lodObj.GetComponent<Slider>();
        this.LODCutoff = lodslider.value;
        PlayerPrefs.SetFloat("worldLodSlider", this.LODCutoff);
        PlayerPrefs.Save();
        updateLOD(useLOD);
    }

    private void applyLOD(GameObject go)
    {
        if (!useLOD)
            return;
        LODGroup group = go.GetComponent<LODGroup>();
        if (group == null)
            group = go.AddComponent<LODGroup>();
        group.animateCrossFading = true;
        group.fadeMode = LODFadeMode.SpeedTree;
        LOD[] lods = new LOD[2];
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        lods[0] = new LOD(LODCutoff, renderers);
        lods[1] = new LOD(1f - LODCutoff, renderers);
        group.SetLODs(lods);


    }
}

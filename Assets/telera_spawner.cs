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
//    TreeDictionary<Guid, NifLoadJob> objsToCreateList;
    GameObject charC;
    ThirdPersonUserControl tpuc;
    Rigidbody tpucRB;
    public GameObject mcamera;
    //camera_movement camMove;
    System.IO.StreamReader fileStream;
    public GameObject telaraObjectPrefab;
    Map map;
    Text zoneText;

  
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


   

    public int ObjJobLoadQueueSize()
    {
        return worldLoader.tCount();
    }

    // Use this for initialization
    void Start()
    {
        // prime the GUID random number generator
        Guid.NewGuid();

        Slider lodslider = GameObject.Find("LODSlider").GetComponent<Slider>();
        this.LODCutoff = PlayerPrefs.GetFloat("worldLodSlider", 0.9f);
        lodslider.value = this.LODCutoff;


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

        MAX_NODE_PER_FRAME = ProgramSettings.get("MAX_NODE_PER_FRAME", 15000);

        setCameraLoc(GameWorld.initialSpawn);
        map = CDRParse.getMap(GameWorld.worldName);
        zoneText = GameObject.Find("ZoneText").GetComponent<Text>();
        foreach (Zone z in map.zones)
        {
            Debug.Log("creating zone:" + z._113Key);
            SCG.List<Vector3> points = z.points;
            GameObject zone = new GameObject("zone:" + z._113Key);
            
            PolygonCollider2D p = zone.AddComponent<PolygonCollider2D>();
            p.points = points.Select(x => new Vector2(x.x, x.z)).ToArray();
            z.collider = p;
            

        }
        foreach (Scene z in map.scenes)
        {
            SCG.List<Vector3> points = z.points;
            GameObject zone = new GameObject("scene:" + z._114Key);

            PolygonCollider2D p = zone.AddComponent<PolygonCollider2D>();
            p.points = points.Select(x => new Vector2(x.x, x.z)).ToArray();
            z.collider = p;


        }

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
    SCG.List<GameObject> invisibleObjects = new SCG.List<GameObject>();
    public void toggleInvisible(bool v)
    {
        /** FindGameObjectsWithTag doesn't work with with "inactive" objects */
        //foreach(GameObject go in GameObject.FindGameObjectsWithTag("invisible"))
        //    go.SetActive(v);
        foreach (GameObject go in invisibleObjects)
            go.SetActive(v);
    }

    void addCDR(ObjectPosition op, GameObject go)
    {
        // add some debug stuff to the object if we are in the editor
        CDRItem cdrItem = go.GetComponent<CDRItem>();
        if (cdrItem == null)
            cdrItem = go.AddComponent<CDRItem>();
        cdrItem.cdrFile = op.cdrfile;
        cdrItem.index = op.index;
        cdrItem.name = op.entityname;
    }
    private GameObject process(ObjectPosition op)
    {

        if (op is LightPosition)
        {
            GameObject lgo = new GameObject();

#if UNITY_EDITOR
            addCDR(op, lgo);
#endif
            LightPosition lp = (LightPosition)op;
            lgo.transform.SetParent(meshRoot.transform);
            lgo.transform.localScale = new Vector3(op.scale, op.scale, op.scale);
            lgo.transform.localPosition = op.min;
            lgo.transform.localRotation = op.qut;

            Light light = lgo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(lp.r, lp.g, lp.b);
            light.intensity = lp.range;
            light.shadows = LightShadows.Hard;
            return lgo;
        }

        GameObject go = GameObject.Instantiate(telaraObjectPrefab, meshRoot.transform);
#if UNITY_EDITOR
        addCDR(op, go);
#endif
        if (!op.visible || (op.nifFile != null && op.nifFile.Contains("30meter.nif")))
        {
            go.tag = "invisible";
            go.SetActive(false);
            invisibleObjects.Add(go);
        }


        string name = op.nifFile;
        Assets.RiftAssets.AssetDatabase.RequestCategory category = Assets.RiftAssets.AssetDatabase.RequestCategory.NONE;
        if (name.Contains("_terrain_") || name.Contains("ocean_chunk"))
        {
            if (name.Contains("_terrain_"))
                category = Assets.RiftAssets.AssetDatabase.RequestCategory.GEOMETRY;
        }

        telara_obj tobj = go.GetComponent<telara_obj>();
        tobj.setProps(category, this);

        //go.transform.SetParent(meshRoot.transform);

        tobj.setFile(name);
        go.name = name;
        go.transform.localScale = new Vector3(op.scale, op.scale, op.scale);
        go.transform.localPosition = op.min;
        go.transform.localRotation = op.qut;
        
        
        triggerLoad(tobj);
        return go;
    }

    WorldLoadingThread worldLoader;

    private Dropdown dropdown;
    void triggerLoad(telara_obj obj)
    {
        if (obj != null)
        {
            if (!(obj.doLoad || obj.loaded))
            {
                obj.doLoad = true;
                worldLoader.addJob(obj, obj.file);
            }
        }
    }
   

    GameObject mount;

    private Vector3 getWorldCamPos()
    {
        return meshRoot.transform.InverseTransformPoint(mcamera.transform.position);
    }

    void OnDestroy()
    {
        if (worldLoader != null)
            worldLoader.doShutdown();
    }

    Zone lastZone = null;
    GameObject zoneSky = null;
    void handleZone()
    {
        Vector3 camPos = getWorldCamPos();
        Vector2 cPos = new Vector2(camPos.x, camPos.z);
        Zone zone = null;
        Scene scene = null;
        foreach (Zone z in map.zones)
            if (z.collider.OverlapPoint(cPos))
                zone = z;
        foreach (Scene s in map.scenes)
            if (s.collider.OverlapPoint(cPos))
                scene = s;
        string zstr = "";
        string cstr = "";
        if (zone != null)
            zstr = zone.name;
        if (scene != null)
            cstr = scene.name;
        zoneText.text = "Zone:" + zstr + ", scene:" + cstr;
        if (zone != lastZone)
        {
            if (zoneSky != null)
            {
                //zone
            }


        }
    }

    // Update is called once per frame
    void Update()
    {
        handleZone();
        if (tpuc != null && tpuc.isRotating)
            return;

        if (Input.GetKeyDown(KeyCode.F) )
            handleMount();

        if (Input.GetKeyDown(KeyCode.P) && GameWorld.useColliders && charC != null)
        {
            setCameraLoc(GameWorld.initialSpawn, true);
        }
        if (worldLoader == null)
        {
            worldLoader = new WorldLoadingThread();
            worldLoader.startThread();
        }
        worldLoader.cameraWorldCamPos = mcamera.transform.position;
        worldLoader.telaraWorldCamPos = getWorldCamPos();
        worldLoader.processThreadsUnityUpdate(processRunningList, process);
    }



    [CallFromUnityUpdate]
    public void processRunningList(TreeDictionary<long, NifLoadJob> runningList)
    {
        foreach (NifLoadJob job in runningList.Values.ToArray())
        {
            if (job.Update())
            {
                //Debug.Log("finalize load:" + job.filename);
                finalizeJob(job);
                runningList.Remove(job.uid);
            }
        }
    }



    [CallFromUnityUpdate]
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

        // don't LOD terrain
        if (go.GetComponent<TerrainObj>() != null)
            return;

        if (!useLOD)
            return;
        LODGroup group = go.GetComponent<LODGroup>();
        if (group == null)
            group = go.AddComponent<LODGroup>();
        group.animateCrossFading = true;
        group.fadeMode = LODFadeMode.SpeedTree;
        LOD[] lods = new LOD[1];
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        lods[0] = new LOD(LODCutoff, renderers);
        //lods[1] = new LOD(1f - LODCutoff, renderers);
        group.SetLODs(lods);


    }

    public static void DestroyChildren(Transform root)
    {
        int childCount = root.childCount;
        for (int i = 0; i < childCount; i++)
        {
            GameObject.Destroy(root.GetChild(0).gameObject);
        }
    }

    void handleMount()
    {
        if (mount == null)
        {
            int key = ProgramSettings.get("MOUNT_KEY", 1445235995); // dragon mount default
            string anim = ProgramSettings.get("MOUNT_ANIM", "mount_dragon_jump_cycle");
            float speed = ProgramSettings.get("MOUNT_ANIM_SPEED", 0.02f);
            mount = AnimatedModelLoader.loadNIF(key);
            AnimatedNif animNif = mount.GetComponent<AnimatedNif>();
            animNif.animSpeed = speed;
            animNif.setSkeletonRoot(mount);
            animNif.setActiveAnimation(anim);
            //mount.transform.parent = mcamera.transform;

            mount.transform.position = this.mcamera.transform.position;
            mount.transform.rotation = this.mcamera.transform.rotation;
            // human_female_mount_dragon_jump_cycle.kf

            GameObject character = new GameObject();

            Paperdoll mainPaperdoll = character.AddComponent<Paperdoll>();
            mainPaperdoll.animOverride = anim;
            mainPaperdoll.kfbOverride = "human_female_mount.kfb";
            mainPaperdoll.setGender("female");
            mainPaperdoll.setRace("human");
            //mainPaperdoll.GetComponent<AnimatedNif>().animSpeed = 0.02f;
            mainPaperdoll.animSpeed = speed;
            character.transform.parent = mount.transform;
            character.transform.localPosition = new Vector3(0, 0, 0);
            character.transform.localRotation = Quaternion.identity;
            mainPaperdoll.transform.localRotation = Quaternion.identity;

            mainPaperdoll.setAppearenceSet(-57952362);

            this.mcamera.GetComponent<cam.camera_movement>().enabled = false;
            mount_movement mm = mount.AddComponent<mount_movement>();
            mm.source = mount;

            this.mcamera.GetComponent<Mount_Camera>().enabled = true;
            this.mcamera.GetComponent<Mount_Camera>().target = mount.transform;


        }
        else
        {
            DestroyChildren(mount.transform);
            GameObject.Destroy(mount);
            mount = null;
            this.mcamera.GetComponent<cam.camera_movement>().enabled = true;
            this.mcamera.GetComponent<Mount_Camera>().enabled = false;
        }

    }
}

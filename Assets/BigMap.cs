using Assets;
using Assets.Database;
using Assets.RiftAssets;
using Assets.WorldStuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.IO;

public class BigMap : MonoBehaviour
{
    public delegate void OnSpawnClickDelegate(WorldSpawn spawn);
    public OnSpawnClickDelegate OnSpawnClick = delegate {};
    public RawImage image;
    public GameObject teleportRoot;
    public GameObject teleportTemplate;
    public GameObject toolTip;
    DB db;
    string world = "world";
    float pixelsPerMeter = 10;
    // Use this for initialization
    void Start()
    {
        DBInst.loadOrCallback((d) => db = d);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            GetComponent<RawImage>().enabled = !GetComponent<RawImage>().enabled;
            foreach (Transform t in teleportRoot.transform)
                GameObject.Destroy(t.gameObject);
            if (GetComponent<RawImage>().enabled)
                updateWorld();
        }
    }

    public void setWorld(string newWorld)
    {
        this.world = newWorld;
    }

    void updateWorld()
    {
        while (db == null) ;
        

        string dds = world + "_map_big_revealed.dds";
        AssetDatabase adb = AssetDatabaseInst.DB;
        byte[] data = adb.extractUsingFilename(dds, AssetDatabase.RequestCategory.TEXTURE);
        image.texture = DDSLoader.DatabaseLoaderTexture_DDS.LoadDDS(data);

        int sizeX = 0;
        int sizeY = 0;
        CDRParse.getMinMax(world, ref sizeX, ref sizeY);
        int tileX = sizeX / 256;
        int tileY = sizeY / 256;

        RectTransform rt = image.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(tileX * pixelsPerMeter, tileY * pixelsPerMeter);

        List<WorldSpawn> spawns = CDRParse.getSpawns(adb, db, world);
        foreach (WorldSpawn spawn in spawns)
        {
            if (spawn.imagePath != null && spawn.imagePath.Length > 0)
            {
                GameObject go = GameObject.Instantiate<GameObject>(teleportTemplate, teleportRoot.transform);
                MapTeleport mt = go.AddComponent<MapTeleport>();
                mt.spawn = spawn;
                RawImage ri = go.GetComponent<RawImage>();
                RectTransform rrt = go.GetComponent<RectTransform>();
                rrt.anchoredPosition = new Vector3((spawn.pos.x / 256.0f) * pixelsPerMeter, ((spawn.pos.z / 256.0f) * pixelsPerMeter), 0);
                go.SetActive(true);

                makeTrigger(go, EventTriggerType.PointerEnter, (x) => OnPointerEnter((PointerEventData)x));
                makeTrigger(go, EventTriggerType.PointerExit, (x) => OnPointerExit((PointerEventData)x));
                //makeTrigger(go, EventTriggerType.PointerDown, (x) => OnPointerClick((PointerEventData)x));
                makeTrigger(go, EventTriggerType.PointerClick, (x) => OnPointerClick((PointerEventData)x));
            }
        }
    }
    
    private void OnPointerClick(PointerEventData x)
    {
        GameObject obj = x.pointerPress;
        MapTeleport mt = obj.GetComponent<MapTeleport>();
        hide();
        OnSpawnClick.Invoke(mt.spawn);
    }

    private void OnPointerExit(PointerEventData x)
    {
        toolTip.SetActive(false);
    }

    private void OnPointerEnter(PointerEventData x)
    {
        try
        {
            toolTip.SetActive(true);
            GameObject obj = x.pointerEnter;
            MapTeleport mt = obj.GetComponent<MapTeleport>();
            RectTransform rw = obj.GetComponent<RectTransform>();

            WorldSpawn spawn = mt.spawn;
            RectTransform rrt = toolTip.GetComponent<RectTransform>();
            rrt.anchoredPosition = new Vector3(((spawn.pos.x / 256.0f) * pixelsPerMeter) + rw.sizeDelta.x / 2, ((spawn.pos.z / 256.0f) * pixelsPerMeter) + rw.sizeDelta.y / 2, 0);

            RawImage ri = toolTip.transform.FindDeepChild("TooltipImage").GetComponent<RawImage>();
            Text txt = toolTip.transform.FindDeepChild("SpawnName").GetComponent<Text>();
            string img = spawn.imagePath;
            string nameOnly = Path.GetFileName(img);
            AssetDatabase adb = AssetDatabaseInst.DB;
            byte[] data = adb.extractUsingFilename(nameOnly, AssetDatabase.RequestCategory.TEXTURE);
            if (nameOnly.EndsWith("dds"))
            {
                ri.texture = DDSLoader.DatabaseLoaderTexture_DDS.LoadDDS(data);
            }
            else if (nameOnly.EndsWith("tga"))
            {
                ri.texture = TGALoader.LoadTGA(new MemoryStream(data));
            }
            else throw new Exception("Unknown texture type:" + nameOnly);
            rrt.sizeDelta = new Vector2(ri.texture.width, ri.texture.height);
            txt.text = spawn.spawnName;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
            toolTip.SetActive(true);
        }
    }

    internal void hide()
    {
        GetComponent<RawImage>().enabled = false;
        foreach (Transform t in teleportRoot.transform)
            GameObject.Destroy(t.gameObject);
        toolTip.SetActive(false);
    }

    public EventTrigger.Entry makeTrigger(GameObject go, EventTriggerType type, UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        EventTrigger.Entry e = new EventTrigger.Entry();
        e.eventID = type;
        e.callback = new EventTrigger.TriggerEvent();
        e.callback.AddListener(action);
        trigger.triggers.Add(e);
        return e;
    }

  
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using Assets.RiftAssets;
using System;
using Ionic.Zlib;
using System.Xml.Serialization;
using Assets.Database;
using System.Threading;
using UnityEngine.UI;
using System.Text;
using CGS;
using UnityEngine.SceneManagement;
using Assets.DatParser;
using Assets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;

public class TestDecomp : MonoBehaviour
{
    bool threaded = true;

    DB db;
    string expectedChecksum;
    bool loaded = false;
    GameObject dropdownbox;
    GameObject loadbutton;
    GameObject loadModelViewerbutton;
    GameObject thirdPersonToggle;
    public GameObject loadWardrobebutton;
    System.Threading.Thread loadThread;
    Text tex;
    Image img;
    string error;
    Color color;
    AssetDatabase adb;
    Dropdown dropdown;

    // Use this for initialization
    void Start()
    {

        tex = GetComponentInChildren<Text>();
        img = GetComponentInChildren<Image>();
        dropdown = GetComponentInChildren<Dropdown>();
        dropdownbox = dropdown.gameObject;
        loadbutton = GameObject.Find("LoadWorldButton");
        loadModelViewerbutton = GameObject.Find("LoadModelButton");
        thirdPersonToggle = GetComponentInChildren<Toggle>().gameObject;
        dropdownbox.SetActive(false);
        thirdPersonToggle.SetActive(false);
        loadbutton.SetActive(false);
        loadModelViewerbutton.SetActive(false);
        color = Color.grey;
        DBInst.progress += (s) => this.error = s;
        DBInst.loadOrCallback((d) => db = d);
        error = "Loading asset database";
        adb = AssetDatabaseInst.DB;
    }

    bool doMapChange = false;
    List<WorldSpawn> worlds = new List<WorldSpawn>();
    public void doModelViewer()
    {
        SceneManager.LoadScene("model_viewer");
    }

    public void loadWardrobe()
    {
        SceneManager.LoadScene("wardrobe");
    }

    private string getLocalized(CObject obj, string defaultText)
    {
        if (obj == null)
            return defaultText;
        if (obj.type != 7703)
            throw new Exception("Not a localizable entry");
        int textID = obj.getIntMember(0);
        return DBInst.lang_inst.getOrDefault(textID, defaultText);
    }
    bool first = false;
    // Update is called once per frame
    void Update()
    {
        if (doMapChange)
        {
            SceneManager.LoadScene("scene1");
            return;
        }
        if (db != null && !first)
        {
            first = true;
            Debug.Log("get keys");
            IEnumerable<entry> keys = db.getEntriesForID(4479);
            worlds.Clear();

            Debug.Log("process keys");
            foreach (entry e in keys)
            {
                byte[] data = e.decompressedData;
                using (MemoryStream ms = new MemoryStream(data))
                {
                    CObject obj = Parser.processStreamObject(ms);
                    string worldName =  obj.getStringMember(0);
                    string internalSpawnName =  obj.getStringMember(1);
                    string spawnName = getLocalized(obj.getMember(10), internalSpawnName);


                    try
                    {
                        Vector3 pos = obj.getVector3Member(2);
                        float angle = angle = obj.getFloatMember(3, 0);
                        pos.y += 2;

                        if (adb.filenameExists(worldName + "_map.cdr"))
                        {
                            worlds.Add(new WorldSpawn(worldName, spawnName, pos, angle));
                        }
                    }catch (Exception ex)
                    {
                        Debug.Log("Unable to get position for spawn [" + e.id + "][" + e.key + "]" + ex);
                    }
                }
            }

            favs.Add("tm_Meridian_EpochPlaza");


            worlds = worlds.OrderBy(w => !favs.Contains(w.spawnName)).ThenBy(w => w.worldName).ThenBy(w => w.spawnName).ToList();

            // do favs first
            List<DOption> options = new List<DOption>();
            foreach (WorldSpawn spawn in worlds)
            {
                DOption option = new DOption(spawn.worldName + " - " + spawn.spawnName + " - " + spawn.pos, spawn);
                options.Add(option);
            }
            dropdown.options.Clear();
            dropdown.AddOptions(options.Cast< Dropdown.OptionData>().ToList());
            dropdown.GetComponent<FavDropDown>().readFavs();

            //dropdown.value = startIndex;
            dropdown.RefreshShownValue();
            dropdownbox.SetActive(true);
            loadbutton.SetActive(true);
            loadModelViewerbutton.SetActive(true);
            loadWardrobebutton.SetActive(true);
            thirdPersonToggle.SetActive(true);
        }
        if (tex != null && img != null)
        {
            tex.text = error;
            img.color = color;
        }
    }

    HashSet<string> favs = new HashSet<string>();

    public static bool abortThread = false;
    public void loadMap()
    {
        if (loaded)
            return;

        if (loadThread != null && loadThread.IsAlive)
        {
            loadThread.Abort();
            abortThread = true;
            error = "Aborted";
        }
        else
        {
            dropdownbox.SetActive(false);
            loadbutton.SetActive(false);
            loadModelViewerbutton.SetActive(false);
            loadWardrobebutton.SetActive(false);
            thirdPersonToggle.SetActive(false);
            //ThirdPersonUIToggle.set
            if (threaded)
            {
                loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(doLoadMap));
                loadThread.Start();
            }
            else
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                doLoadMap();
                watch.Stop();
                Debug.Log("doLoadMap in " + watch.ElapsedMilliseconds + " ms");
            }
        }
         
    }

   

    public void doLoadMap()
    {
        Assets.GameWorld.Clear();
        WorldSpawn spawn = (WorldSpawn)((DOption)dropdown.options[dropdown.value]).userObject;

        string worldName = spawn.worldName;
        string worldCDR = worldName + "_map.cdr";
        Assets.GameWorld.worldName = worldName;
        Assets.WorldStuff.CDRParse.getMinMax(worldCDR, ref Assets.GameWorld.maxX, ref Assets.GameWorld.maxY);

        Assets.GameWorld.initialSpawn = spawn;
        foreach (WorldSpawn s in worlds)
            if (s.worldName.Equals(spawn.worldName))
                Assets.GameWorld.AddSpawns(s);
        doMapChange = true;
    }


    public void OnDestroy()
    {
        if (loadThread != null && loadThread.IsAlive)
            loadThread.Abort();
    }

    public DOption[] getOptions()
    {
        throw new NotImplementedException();
    }
}

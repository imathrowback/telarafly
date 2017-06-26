
using System.Collections;
using System.Collections.Generic;
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
                    String worldName = "" + obj.members[0].convert();
                    String spawnName = "" + obj.members[1].convert();
                    try
                    {
                        Vector3 pos = obj.members[2].readVec3();
                        float angle = 0;
                        pos.y += 2;

                        if (obj.members.Count >= 3)
                        {
                            CObject o = obj.members[3];
                            if (o.getConvertor() is CFloatConvertor)
                                angle = (float)o.convert();
                        }

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
            worlds.Sort();
            Debug.Log("do box");
            dropdown.options.Clear();
            int startIndex = 0;
            int i = 0;
            foreach (WorldSpawn spawn in worlds)
            {
                //if (spawn.worldName.Equals("world") && spawn.spawnName.Equals("tm_exodus_exit"))
                if (spawn.spawnName.Equals("tm_Meridian_EpochPlaza"))
                    startIndex = i;
                Dropdown.OptionData option = new Dropdown.OptionData(spawn.worldName + " - " + spawn.spawnName + " - " + spawn.pos);
                dropdown.options.Add(option);
                i++;
            }
            dropdown.value = startIndex;
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
        WorldSpawn spawn = worlds[dropdown.value];

        string worldName = spawn.worldName;
        string worldCDR = worldName + "_map.cdr";
        Assets.GameWorld.worldName = worldName;
        Assets.WorldStuff.CDRParse.getMinMax(worldCDR, ref Assets.GameWorld.maxX, ref Assets.GameWorld.maxY);

        Assets.GameWorld.initialSpawn = spawn;
        foreach (WorldSpawn s in worlds)
            if (s.worldName.Equals(spawn.worldName))
                Assets.GameWorld.AddSpawns(s);
        doMapChange = true;
        /*
        try
        {
            Debug.Log("Load map");
            error = "Load map";
            Assets.GameWorld.Clear();

            WorldSpawn spawn = worlds[dropdown.value];

            string worldName = spawn.worldName;
            string worldCDR = worldName + "_map.cdr";
            int maxX = 0;
            int maxY = 0;
            error = "get min max";
            Debug.Log("get min/max");
            Assets.WorldStuff.CDRParse.getMinMax(worldCDR, ref maxX, ref maxY);

            Debug.Log("got min/max: [" + maxX + "][" + maxY + "]");


            error = "build spawns";
           

            int total = ((maxX+256) / 256) * ((maxY+256) / 256);
         
            int i = 0;

            Action<ObjectPosition> addFunc = (o) => Assets.GameWorld.Add(o);

            Queue<CDRJob> cdrJobs = new Queue<CDRJob>();

            error = "Enqueing jobs";
            Debug.Log(error);
            for (int x = 0; x <= maxX; x += 256)
            {
                for (int y = 0; y <= maxY; y += 256)
                {
                    CDRJob job = new CDRJob(adb, db, spawn, x, y, addFunc);
                    job.doneFunc = (v) => { i++;
                        error = "Loading " + spawn.worldName + "  -  " + (int)(((float)i / (float)total) * 100.0) + " %";
                    };
                    cdrJobs.Enqueue(job);
                }
            }

            error = "Doing CDRs";
            //Debug.Log("do cdrs");
            int currentThreads = 0;

            List<CDRJob> runningJobs = new List<CDRJob>();
            while (cdrJobs.Count > 0 || runningJobs.Count > 0)
            {
                while (currentThreads < 4 && cdrJobs.Count > 0)
                {
                    Interlocked.Increment(ref currentThreads);
                    CDRJob job = cdrJobs.Dequeue();
                    //Debug.Log("job [" + job.x + "," + job.y + "], starting");
                    if (threaded)
                        job.Start(System.Threading.ThreadPriority.Normal);
                    else
                        job.Run();
                    runningJobs.Add(job);
                }
                foreach (CDRJob j in runningJobs.ToArray())
                {
                    if (j.Update())
                    {
                        Interlocked.Decrement(ref currentThreads);
                        //Debug.Log("job [" + j.x + "," + j.y + "], finished");
                        runningJobs.Remove(j);
                    }
                }
                if (threaded)
                    Thread.Sleep(10);
            }

            Debug.Log("scene change");
            doMapChange = true;
        }
        catch (ThreadAbortException ex)
        {
            return;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Debug.Log(ex);
            throw ex;
        }
        */
    }

    /*
    class CDRJob : ThreadedJob
    {
        WorldSpawn spawn;
        public int x;
        public int y;
        Action<ObjectPosition> addFunc;
        AssetDatabase adb;
        DB db;
        public Action<int> doneFunc;

        public CDRJob(AssetDatabase adb, DB db, WorldSpawn spawn, int x, int y, Action<ObjectPosition> addFunc)
        {
            this.adb = adb;
            this.db = db;
            this.spawn = spawn;
            this.x = x;
            this.y = y;
            this.addFunc = addFunc;
        }

        protected override void ThreadFunctionCDR()
        {
            try
            {
                Assets.WorldStuff.CDRParse.doCDR(adb, db, spawn.worldName, x, y, addFunc);
            }
            catch (Exception ex)
            {
                //Debug.Log("Exception trying to do job[" + x + "," + y + "]");
                //Debug.Log(ex);
                //IsDone = true;
            }
        }
        protected override void OnFinished()
        {
            if (doneFunc != null)
                doneFunc.Invoke(0);
        }
    }
    */
   

    public void OnDestroy()
    {
        if (loadThread != null && loadThread.IsAlive)
            loadThread.Abort();
    }
   




   

    sealed class PreMergeToMergedDeserializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type typeToDeserialize = null;

            // For each assemblyName/typeName that you want to deserialize to
            // a different type, set typeToDeserialize to the desired type.
            String exeAssembly = Assembly.GetExecutingAssembly().FullName;


            // The following line of code returns the type.
            typeToDeserialize = Type.GetType(String.Format("{0}, {1}", typeName, exeAssembly));

            return typeToDeserialize;
        }
    }


}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Assets.RiftAssets;
using System;
using Ionic.Zlib;
using System.Xml.Serialization;
using Assets.DB;
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
    System.Diagnostics.Process pr;
    DB db;
    string expectedChecksum;
    bool loaded = false;
    System.Threading.Thread loadThread;
    GameObject dropdownbox;
    GameObject loadbutton;
    GameObject loadModelViewerbutton;
    GameObject thirdPersonToggle;
    Text tex;
    Image img;
    string error;
    Color color;
    AssetDatabase adb;
    Dropdown dropdown;
    string assetsManifest;
    string assetsManifest32;
    string assetsDirectory;

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
        loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(readDB));
        loadThread.Start();
    }

    void loadManifestAndDB()
    {
        Properties p = new Properties("nif2obj.properties");
        assetsDirectory = (p.get("ASSETS_DIR"));
        assetsManifest = (p.get("ASSETS_MANIFEST64"));
        assetsManifest32 = (p.get("ASSETS_MANIFEST"));

        error = "Loading manifest";
        Manifest manifest = new Manifest(assetsManifest32);
        error = "Loading asset database";
        adb = AssetProcessor.buildDatabase(manifest, assetsDirectory);
        AssetEntry ae = adb.getEntryForFileName("telara.db");
        expectedChecksum = BitConverter.ToString(ae.hash);
    }
    void readDB()
    {
        UnityEngine.Debug.Log("Begin db load in thread");
        try
        {
            loadManifestAndDB();

            AssetEntry ae = adb.getEntryForFileName("telara.db");
            expectedChecksum = BitConverter.ToString(ae.hash);

            error = "read database";
            Debug.Log("read database");
            db = DBInst.readDB(expectedChecksum, (s) => error = s);
            Debug.Log("Db:" + db);
            if (db != null)
            {
                loaded = true;
            }
            else
            {
                error = "Decode database, please wait, this could take a few minutes but only needs to be done once per patch.";

                string file = @"decomp\tdbdecomp.exe";
                pr = new System.Diagnostics.Process();
                pr.StartInfo.FileName = file;
                pr.StartInfo.Arguments = "\"" + assetsManifest32 + "\" \"" + assetsDirectory + "\"";
                pr.Start();
                pr.WaitForExit();
                db = DBInst.readDB(expectedChecksum, (s)=>error = s);
                loaded = true;
            }
            UnityEngine.Debug.Log("Load complete");
            error = "Select world to load";
            color = Color.green;
        }
        catch (Exception ex)
        {

            color = Color.magenta;
            error = "There was an error. Please exit and check output_log.txt in the data directory";
            UnityEngine.Debug.LogWarning(ex);
        }
    }

    

    bool doMapChange = false;
    List<WorldSpawn> worlds = new List<WorldSpawn>();
    public void doModelViewer()
    {
        SceneManager.LoadScene("model_viewer");
    }
    // Update is called once per frame
    void Update()
    {
        if (doMapChange)
        {
            SceneManager.LoadScene("scene1");
            return;
        }
        if (loaded)
        {
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
                        Vector3 pos = readVec3(obj.members[2]);
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
                        Debug.Log("Unable to get position for spawn [" + e.id + "][" + e.key + "]");
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
            loaded = false;
            dropdownbox.SetActive(true);
            loadbutton.SetActive(true);
            loadModelViewerbutton.SetActive(true);
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

        if (loadThread != null & loadThread.IsAlive)
        {
            loadThread.Abort();
            abortThread = true;
            error = "Aborted";
        }
        else
        {
            abortThread = false;

            dropdownbox.SetActive(false);
            loadbutton.SetActive(false);
            loadModelViewerbutton.SetActive(false);
            thirdPersonToggle.SetActive(false);
            //ThirdPersonUIToggle.set
            loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(doLoadMap));
            loadThread.Start();
        }
    }

    public void doLoadMap()
    {
        try
        {
            Debug.Log("Load map");
            Assets.GameWorld.Clear();

            WorldSpawn spawn = worlds[dropdown.value];
            int startX = 0;
            int startY = 0;
            int maxX = 0;
            int maxY = 0;
            for (int x = 0; x < 50176; x += 256)
            {
                for (int y = 0; y < 50176; y += 256)
                {
                    string s = spawn.worldName + "_" + x + "_" + y + ".cdr";
                    if (adb.filenameExists(s))
                    {
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                    else
                        break;
                }
             }

            Assets.GameWorld.initialSpawn = spawn;
            foreach (WorldSpawn s in worlds)
                if (s.worldName.Equals(spawn.worldName))
                    Assets.GameWorld.AddSpawns(s);


            /*
             * A_C_keep_stillmoor_south_entry_01.nif
            */
            /*
            maxX = startX = 1792;
            maxY = startY = 3328;
            Assets.GameWorld.initialSpawn = new WorldSpawn("world", "walls", new Vector3(1855, 1188, 3393), Mathf.Deg2Rad * 511);
            */
            int total = ((maxX+256) / 256) * ((maxY+256) / 256);
         
            int i = 0;

            Action<ObjectPosition> addFunc = (o) => Assets.GameWorld.Add(o);

            Queue<CDRJob> cdrJobs = new Queue<CDRJob>();
            error = "Enqueing jobs";
            for (int x = startX; x <= maxX; x += 256)
            {
                for (int y = startY; y <= maxY; y += 256)
                {
                    //doCDR(spawn, x, y, addFunc);
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
            while (cdrJobs.Count > 0)
            {
                if (currentThreads < 10)
                {
                    Interlocked.Increment(ref currentThreads);
                    CDRJob job = cdrJobs.Dequeue();
                    //Debug.Log("job [" + job.x + "," + job.y + "], starting");
                    job.Start(System.Threading.ThreadPriority.Normal);
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
                Thread.Sleep(10);
            }

            Debug.Log("scene change");
            doMapChange = true;
        }
        catch (ThreadAbortException ex)
        {
            return;
        }
    }

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

        protected override void ThreadFunction()
        {
            try
            {
               // Debug.Log("Begin threaded CDR[" + x + "," + y + "]");
                doCDR(adb, db, spawn, x, y, addFunc);
               // Debug.Log("Done threaded CDR[" + x + "," + y + "], ready for update to be called");
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

    public static void doCDR(AssetDatabase adb, DB db, WorldSpawn spawn, int x, int y, Action<ObjectPosition> addFunc)
    {
        string s = spawn.worldName + "_" + x + "_" + y + ".cdr";

        
        try
        {
            if (abortThread)
                return;
            processCDR(s, addFunc, adb, db);
            // also add the terrain nif!
            String type = "_split";
            String terrainNif = String.Format("{0}_terrain_{1}_{2}{3}.nif", spawn.worldName, x, y, type);
            if (adb.filenameExists(terrainNif))
            {
                Vector3 pos = new Vector3(x, 0.0f, y);
                addFunc.Invoke(new ObjectPosition(terrainNif, pos, Quaternion.identity, pos, 1.0f));
            }

        }
        catch (ThreadAbortException ex)
        {
            UnityEngine.Debug.Log("Unable to process CDR:" + s + " due to error:" + ex.Message);
            return;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log("Unable to process CDR:" + s + " due to error:" + ex.Message);
        }
    }

    public void OnDestroy()
    {
        if (loadThread != null & loadThread.IsAlive)
            loadThread.Abort();
    }
    static void processCDR(String str, Action<ObjectPosition> addFunc, AssetDatabase adb, DB db)
    {
        if (!adb.filenameExists(str))
            return;
        AssetEntry ae = adb.getEntryForFileName(str);
        byte[] data = adb.extract(ae);
        if (data[0] != 0x6B)
        {
            UnityEngine.Debug.Log("Unknown code " + data[0] + ", expected:" + 0x6b);
            return;
        }
        processCDR(new MemoryStream(data), str, addFunc, db);

    }

    static void  processCDR(Stream ms, string cdrName, Action<ObjectPosition> addFunc, DB db)
    {
        try
        {
            CObject obj = Parser.processStreamObject(ms);

            if (obj.type != 107)
                throw new Exception("CDR file was not class 107");

            String oname = "";

            List<CObject> members = obj.members;
            if (members.Count > 0)
            {
                CObject first = members[0];
                if (first.type == 11)
                {
                    foreach (CObject child in first.members)
                    {
                        if (child.type == 600)
                        {
                            List<CObject> cMembers = child.members;

                            CObject index = cMembers[0];
                            if (cMembers.Count > 1)
                            {
                                CObject nameObj = cMembers[1];
                                CStringConvertor sconv = (CStringConvertor)nameObj.getConvertor();
                                oname = (string)sconv.convert(nameObj);
                                CObject ary = null;
                                if (cMembers.Count == 3)
                                    ary = cMembers[2];
                                else if (cMembers.Count == 4)
                                {
                                    String setdec = cMembers[2].get(0).get(0).convert() + "";
                                    // System.out.println(setdec);
                                    ary = cMembers[3];
                                }
                                else
                                {
                                    // dunno, guess?
                                    foreach (CObject o in cMembers)
                                        if (o.members.Count == 4)
                                            ary = o;
                                }
                                if (null == ary)
                                    throw new Exception("Unable to handle cMembers size:" + cMembers.Count);
                                // child members in ary 602 and 603 contain references into the database under id 623
                                // they point to object 628 which contains references to the actual NIF/HKX files
                                long nif_hkx_ref = long.MaxValue;
                                CObject _602 = findFirstType(ary, 602);
                                if (_602 == null)
                                {
                                    UnityEngine.Debug.Log("no nif ref found for :" + oname);
                                }
                                else
                                {
                                    try
                                    {
                                        nif_hkx_ref = Convert.ToInt64(_602.get(0).convert());
                                        CObject _603 = findFirstType(ary, 603);

                                        Vector3 min = readVec3(_603.members[1]);
                                        Quaternion qut = readQuat(_603.members[2]);

                                        float unkValue = 0;
                                        int _3index = 3;
                                        Vector3 max = new Vector3();
                                        float scale = 1.0f;
                                        if (_603.members.Count >= 4)
                                        {
                                            if (_603.members[3].type == 11)
                                                max = readVec3(_603.members[3]);
                                            else
                                            {
                                                //System.out.println(_603.members.get(3).convert());
                                                if (_603.members.Count >= 5
                                                        && _603.members[4].type == 11)
                                                {
                                                    scale = (float)(new CFloatConvertor().convert(_603.members[3]));
                                                    max = readVec3(_603.members[4]);
                                                }
                                            }
                                        }
                                        if (nif_hkx_ref != long.MaxValue)
                                        {
                                            CObject dbObj = getDBObj(db, 623, nif_hkx_ref);
                                            if (dbObj != null)
                                            {
                                                CObject dbAry = dbObj.get(0);
                                                CObject _7319 = findFirstType(dbAry, 7319);
                                                CObject _7318 = findFirstType(dbAry, 7318);
                                                if (_7319 != null)
                                                {
                                                    long nifKey = Convert.ToInt64(_7319.get(0).convert());
                                                    CObject _7305Obj = getDBObj(db, 7305, nifKey);
                                                    String nif = "" + _7305Obj.members[0].convert();

                                                    string nifFile = Path.GetFileName(nif);

                                                    addFunc.Invoke(new Assets.ObjectPosition(nifFile, min, qut, max, scale));
                                                    
                                                }
                                            }

                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Log(ex);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log("exception trying to process CDR:" + cdrName);
            Debug.Log(ex);
        }
        finally
        {
            //Debug.Log("process cdr[" + cdrName + "]: done in " + watch.ElapsedMilliseconds + " ms");
        }
       return;
    }

    static private CObject getDBObj(DB db, long id, long key)
    {
        if (!db.hasEntry(id, key))
            return null;
        byte[] dbData = db.getData(id, key);
        CObject obj = Parser.processStreamObject(new MemoryStream(dbData));

        return obj;
    }
    private static Quaternion readQuat(CObject cObject)
    {
        if (cObject.members.Count != 4)
            throw new Exception("Not arrary of 4 was ary of :" + cObject.members.Count);
        CFloatConvertor conv = new CFloatConvertor();
        float a = (float)conv.convert(cObject.members[0]);
        float b = (float)conv.convert(cObject.members[1]);
        float c = (float)conv.convert(cObject.members[2]);
        float d = (float)conv.convert(cObject.members[3]);
        return new Quaternion(a, b, c, d);
    }

    private static Vector3 readVec3(CObject cObject)
    {
        if (cObject.members.Count != 3)
            throw new Exception("Not arrary of 3 was ary of :" + cObject.members.Count);
        CFloatConvertor conv = new CFloatConvertor();
        try
        {
            return new Vector3((float)conv.convert(cObject.members[0]), (float)conv.convert(cObject.members[1]),
                   (float)conv.convert(cObject.members[2]));
        }
        catch (Exception e)
        {
            return new Vector3();
        }
    }

    private static CObject findFirstType(CObject ary, int i)
    {
        foreach (CObject child in ary.members)
            if (child.type == i)
                return child;

        return null;
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

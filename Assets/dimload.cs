using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets.Export;
using Assets.RiftAssets;
using Assets;
using System.IO;
using Assets.Wardrobe;
using Assets.Database;
using Assets.NIF;
using Assets.DatParser;
using UnityEngine.SceneManagement;
using System;

public class dimload : MonoBehaviour {
    Transform root;
    AssetDatabase adb;
    DB db;
    bool loaded = false;
    // Use this for initialization
    void Start () {
        adb = AssetDatabaseInst.DB;
        DBInst.loadOrCallback((d) => db = d);
        root = GameObject.Find("ROOT").transform;

    }

    static Vector3 parseV(string x)
    {
        string []v = x.Split(',');
        return new Vector3(
            float.Parse(v[0]),
            float.Parse(v[1]),
            float.Parse(v[2])
            );
    }

    static Quaternion parseQ(string x)
    {
        string[] q = x.Split(',');
        return new Quaternion(
            float.Parse(q[0]),
            float.Parse(q[1]),
            float.Parse(q[2]),
            float.Parse(q[3])
            );
    }
    static Quaternion parseM(string x)
    {
        try
        {
            string[] q = x.Split(',');
            float[] qq = new float[]
            {
            float.Parse(q[0]),            float.Parse(q[1]),            float.Parse(q[2]),
            float.Parse(q[3]),            float.Parse(q[4]),            float.Parse(q[5]),
            float.Parse(q[6]),            float.Parse(q[7]),            float.Parse(q[8]),

            };
            Matrix4x4 m = new Matrix4x4();

            m.SetRow(0, new Vector4(qq[0], qq[1], qq[2], 0.0f));
            m.SetRow(1, new Vector4(qq[3], qq[4], qq[5], 0.0f));
            m.SetRow(2, new Vector4(qq[6], qq[7], qq[8], 0.0f));
            //m.SetColumn(0, new Vector4(qq[0], qq[1], qq[2], 1.0f));

            return m.rotation;
        }
        catch (Exception e)
        {
            return Quaternion.identity;
        }
        
    }
    static bool once = false;
    public static void doIPCLoad(Action< ObjectPosition> func)
    {
        if (true)
            return;
    if (once)
        return;
    once = true;
    string f = @"meridian_data";
        var r = Resources.Load<TextAsset>("meridian_data");
        //StringReader ss = new StringReader(r.text);

    if (r != null)
            //File.Exists(f) )
    {
            Debug.Log("Loading meridian data");
            StringReader sr = new StringReader(r.text);
        string line;

        while ((line = sr.ReadLine()) != null)
        {
            try
            {
                string[] parts = line.Split(':');
                Vector3 v = parseV(parts[0]);
                float scale = float.Parse(parts[1]);
                Quaternion q = parseM(parts[2]);
                string s = parts[3];
                string id = parts[4];

                ObjectPosition op = new ObjectPosition(Path.GetFileName(s), v, q, v, scale);
                op.id = id;
                op.memmerObject = true;

                func.Invoke(op);
                //GameWorld.staticObjects.Add(op);
            }catch(Exception e)
            {

            }
        }

        sr.Close();
    }
    else
            Debug.LogWarning("Unable to load meridian data, resource file not found");



        /*
        if (once)
            return;
        once = true;
        string ready = @"l:\temp\mem_ipc\ready";
        string f = @"l:\temp\mem_ipc\data";
        if (File.Exists(f) && File.Exists(ready))
        {
            StreamReader sr = new StreamReader(f);
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                try
                {
                    string[] parts = line.Split(':');
                    Vector3 v = parseV(parts[0]);
                    float scale = float.Parse(parts[1]);
                    Quaternion q = parseM(parts[2]);
                    string s = parts[3];
                    string id = parts[4];

                    ObjectPosition op = new ObjectPosition(Path.GetFileName(s), v, q, v, scale);
                    op.id = id;
                    op.memmerObject = true;

                    func.Invoke(op);
                    //GameWorld.staticObjects.Add(op);
                }catch(Exception e)
                {

                }
            }

            sr.Close();

            //File.Delete(f);
            //File.Delete(ready);
        }
        */
    }

    // Update is called once per frame
    void Update () {
        if (db != null && !loaded)
        {
            loaded = true;

            //doIPCLoad();
            WorldSpawn ws = new WorldSpawn("guardian_map", "tm_guardian_map_start", new Vector3(1120.1f,906.3f,1409.0f), 0);
            // guardian_map
            // tm_guardian_map_start
            // 1120.14 906.31 1409.01
            //WorldSpawn ws = new WorldSpawn("world", "unknown", new Vector3(1120.1f,906.3f,1409.0f), 0);
            Assets.GameWorld.initialSpawn = ws;
               
            Assets.GameWorld.AddSpawns(ws);
            Assets.GameWorld.worldName = "guardian_map";
            SceneManager.LoadScene("scene1");
            //NIFLoader.loadNIF("human_female_tail_001.nif");

        }
        NIFTexturePool.inst.process();
    }
}

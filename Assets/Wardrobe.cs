using Assets;
using Assets.DB;
using Assets.RiftAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using deep
public class Wardrobe : MonoBehaviour
{
    DB db;
    GameObject root;
    System.Threading.Thread loadThread;
    NIFLoader loader;
    public Text text;
    AssetDatabase adb;
    string progress;

    public float animSpeed = 0.02f;

    // Use this for initialization
    void Start()
    {
        root = GameObject.Find("ROOT");

        loader = new NIFLoader();
        loader.loadManifestAndDB();
        adb = loader.db;

        string nif = "human_male.nif";
        string kfm = "human_male.kfm";
        string kfb = "human_male.kfb";

        this.animationNif = new Assets.AnimatedNif(adb, nif, kfm, kfb);

        GameObject go = loader.loadNIF(nif);
        go.transform.parent = root.transform;

        process(loader.loadNIF("human_male_plate_foot_403.nif", true));
        process(loader.loadNIF("human_male_plate_hand_403.nif", true));

        loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(loadDatabase));
        loadThread.Start();
    }

    void process(GameObject wings)
    { 
        Transform wingsTransform = wings.transform.GetChild(0);
        int wC = wingsTransform.childCount;
        Debug.Log("wc:" + wC);
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < wC; i++)
            children.Add(wingsTransform.GetChild(i));
        for (int i = 0; i < children.Count; i++)
        {
            Transform child = children[i];
            Debug.Log("found child:" + child.name);
            if (child.name.Contains("JNT"))
            {
                Transform existChild = root.transform.FindDeepChild(child.name);
                if (existChild != null)
                {
                    // replace the existing node with the new one
                    child.parent = existChild.parent;
                    GameObject.Destroy(existChild.gameObject);

                    //child.parent = existChild;

                }
            }
        }
        

    }

    void loadDatabase()
    {
        AssetEntry ae = adb.getEntryForFileName("telara.db");
        string expectedChecksum = BitConverter.ToString(ae.hash);
        db = DBInst.readDB(expectedChecksum, (s) => { progress = s; });
        if (db == null)
        {
            DBInst.create(AssetDatabaseInst.ManifestFile, AssetDatabaseInst.AssetsDirectory);
            db = DBInst.readDB(expectedChecksum, (s) => { progress = s; });
        }

    }
    // Update is called once per frame
    void Update()
    {
        if (text != null)
            text.text = progress;

        if (loadThread != null)
        {
            if (loadThread.IsAlive)
            {
                return;
            }
            if (db != null)
            {
                loadThread = null;
            }
        }
    }

    float tt = 0;
    private AnimatedNif animationNif;

    void FixedUpdate()
    {
        tt += animSpeed;
        if (tt > 1)
            tt = 0;
        if (animationNif != null)
            animationNif.doFrame(tt);
    }
}

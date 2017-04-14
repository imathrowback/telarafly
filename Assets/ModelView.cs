using Assets.NIF;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Ionic.Zlib;
using System;
using System.Reflection;
using Assets.DB;
using Assets.RiftAssets;
using UnityEngine.UI;
using Assets.DatParser;
using Assets;

public class ModelView : MonoBehaviour
{
    public float animSpeed = 0.02f;
    public int animToUse = 0;
    NIFLoader loader;
    int lastAnimToUse = -1;
    GameObject root;
    GameObject nifmodel;
    AnimatedNif animationNif;
    Text progressText;
    DB db;
    Slider speedSlider;
    AssetDatabase adb;
    Dropdown nIFModelDropdown;
    Dropdown animationDropdown;
    System.Threading.Thread loadThread;
    Dictionary<String, AnimatedNif> nifDictionary = new Dictionary<string, AnimatedNif>();
    volatile string progress = "";
    void Start()
    {
        root = GameObject.Find("ROOT");
        progressText = GameObject.Find("ProgressText").GetComponent<Text>();
        nIFModelDropdown = GameObject.Find("NIFModelDropdown").GetComponent<Dropdown>();
        animationDropdown = GameObject.Find("AnimationDropdown").GetComponent<Dropdown>();
        speedSlider = GameObject.Find("SpeedSlider").GetComponent<Slider>();
        speedSlider.value = this.animSpeed;
        loader = new NIFLoader();
        loader.loadManifestAndDB();
        adb = AssetDatabaseInst.DB;


        loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(loadDatabase));
        loadThread.Start();
    }

    void loadDatabase()
    {
        AssetEntry ae = adb.getEntryForFileName("telara.db");
        string expectedChecksum = BitConverter.ToString(ae.hash);
        db = DBInst.readDB(expectedChecksum, (s) => { progress = s; });


    }

    private void parse(IEnumerable<entry> entries)
    {
        nIFModelDropdown.ClearOptions();
        List<String> nIFModelEntries = new List<String>();
        foreach (entry e in entries)
        {
            if (e.key == -79253527)
                Debug.Log("process hellbug");
            try
            {
                CObject obj = Parser.processStreamObject(new MemoryStream(e.decompressedData));
                if (e.key == -79253527)
                    Debug.Log("process hellbug:" + obj.members.Count);
                if (obj.members.Count >= 2)
                {
                    if (obj.get(0).type == 6 && obj.get(1).type == 6)
                    {
                        string postfix = "";
                        string kfm = obj.get(0).convert() + "";
                        string nif = obj.get(1).convert() + "";
                        //int soundBank = obj.get(2).convert()
                        for (int j = 2; j < obj.members.Count; j++)
                        {
                            if (obj.get(j).type == 6 && ("" + obj.get(j).convert()).StartsWith("_"))
                                postfix = "" + obj.get(j).convert();
                        }
                        string nifFile = Path.GetFileNameWithoutExtension(nif) + ".nif";
                        string kfmFile = Path.GetFileNameWithoutExtension(kfm) + ".kfm";
                        string kfbFile = Path.GetFileNameWithoutExtension(kfm) + postfix + ".kfb";
                        bool nifexists = adb.filenameExists(nifFile);
                        bool kfbexists = adb.filenameExists(kfbFile);
                        if (!(!nifexists || !kfbexists))
                        {
                            string displayName = nifFile;
                            // special handling for mounts as we want them grouped together
                            if (postfix.Length > 0 && postfix.Contains("mount"))
                                displayName = postfix.Replace("_","") + ":" + nifFile;
                            nIFModelEntries.Add(displayName);
                            nifDictionary[nifFile] = new AnimatedNif(adb, nifFile, kfmFile, kfbFile);
                        }
                        if (nif.Contains("mount_") && nif.Contains("hellbug"))
                        {
                            Debug.Log("nif[" + nifexists + "][" + nifFile + "], kfb[" + kfbexists + "]:" + kfbFile);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to parse entry " + e.id + ":" + e.key + ":" + ex.Message);
            }
        }
        nIFModelEntries.Sort();
        nIFModelDropdown.AddOptions(nIFModelEntries);
    }

    static object getField(object obj, string fieldName)
    {
        Type type = obj.GetType();
        FieldInfo field = type.GetField(fieldName);
        if (field != null)
            return field.GetValue(obj);
        return null;
    }

    public void changeNif(string newNifP)
    {
        string newNif = newNifP;
        if (newNifP.Contains(":"))
            newNif = newNifP.Split(':')[1];
        AnimatedNif animNif = nifDictionary[newNif];

        if (nifmodel != null)
            GameObject.DestroyImmediate(nifmodel);

        nifmodel = loader.loadNIF(animNif.nif, true);
        nifmodel.transform.parent = root.transform;

        this.animationDropdown.ClearOptions();
        List<String> anims = new List<String>();
        foreach (KFAnimation ani in animNif.getAnimations())
        {
            anims.Add(ani.sequencename);
        }
        anims.Sort();
        animationNif = animNif;
        this.animationDropdown.AddOptions(anims);
    }
    public void changeAnim()
    {
        changeNif(nIFModelDropdown.options[nIFModelDropdown.value].text);

        string anim = this.animationDropdown.options[this.animationDropdown.value].text;
        animationNif.setActiveAnimation(anim);
    }

    public void changeSpeed()
    {
        animSpeed = speedSlider.value;
    }

    public void changeNIF()
    {
        int value = nIFModelDropdown.value;
        String nif = nIFModelDropdown.options[value].text;
        changeNif(nif);


    }

    // Update is called once per frame
    float tt = 0;
    void FixedUpdate()
    {
        progressText.text = progress;
        if (loadThread != null)
        {
            if (loadThread.IsAlive)
            {
                return;
            }
            if (db != null)
            {
                IEnumerable<entry> entries = db.getEntriesForID(7305);
                parse(entries);
                changeNif("crucia.nif");
                animationNif.setActiveAnimation(0);
                loadThread = null;
            }
        }
        tt += animSpeed;
        if (tt > 1)
            tt = 0;
        if (animationNif != null)
            animationNif.doFrame(tt);
        //doFrame(tt);

    }


}

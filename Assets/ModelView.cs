using Assets.NIF;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Ionic.Zlib;
using System;
using System.Reflection;
using Assets.Database;
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
    Slider speedSlider;
    AssetDatabase adb;
    public GameObject ground;
    Dropdown nIFModelDropdown;
    Dropdown animationDropdown;

    Dropdown AFdropdown;
    Dropdown GLdropdown;
    Dropdown MRdropdown;
    Dropdown SZdropdown;
    Dropdown SZZdropdown;
    Dropdown SZZZdropdown;
    Dictionary<String, AnimatedNif> nifDictionary = new Dictionary<string, AnimatedNif>();
    DB db;
    volatile string progress = "";
    void Start()
    {
        root = GameObject.Find("ROOT");
        progressText = GameObject.Find("ProgressText").GetComponent<Text>();
        nIFModelDropdown = GameObject.Find("NIFModelDropdown").GetComponent<Dropdown>();
        animationDropdown = GameObject.Find("AnimationDropdown").GetComponent<Dropdown>();

        AFdropdown = GameObject.Find("AFdropdown").GetComponent<Dropdown>();
        GLdropdown = GameObject.Find("GLdropdown").GetComponent<Dropdown>();
        MRdropdown = GameObject.Find("MRdropdown").GetComponent<Dropdown>();
        SZdropdown = GameObject.Find("SZdropdown").GetComponent<Dropdown>();
        SZZdropdown = GameObject.Find("SZZdropdown").GetComponent<Dropdown>();
        SZZZdropdown = GameObject.Find("SZZZdropdown").GetComponent<Dropdown>();
        speedSlider = GameObject.Find("SpeedSlider").GetComponent<Slider>();
        speedSlider.value = this.animSpeed;
        loader = new NIFLoader();
        loader.loadManifestAndDB();
        adb = AssetDatabaseInst.DB;

        DBInst.loadOrCallback((d) => db = d);
        DBInst.progress += (m) => progress = m;
    }

    private void parse7305(IEnumerable<entry> entries)
    {
        nIFModelDropdown.ClearOptions();

        this.AFdropdown.ClearOptions();
        this.GLdropdown.ClearOptions();
        this.MRdropdown.ClearOptions();
        this.SZdropdown.ClearOptions();
        this.SZZdropdown.ClearOptions();
            this.SZZZdropdown.ClearOptions();
        List<string> nIFModelEntries = new List<string>();
        List<string> AFdropdownE = new List<string>();
        List<string> GLdropdownE = new List<string>();
        List<string> MRdropdownE = new List<string>();
        List<string> SZdropdownE = new List<string>();
        List<string> SZZdropdownE = new List<string>();
             List<string> SZZZdropdownE = new List<string>();
        List<entry> lentries = new List<entry>(entries);
        List<string>[] buckets = new List<string>[] { AFdropdownE , GLdropdownE, MRdropdownE, SZdropdownE , SZZdropdownE ,SZZZdropdownE };

        //nIFModelEntries.Add("human_male_ref.nif");
        //nifDictionary["human_male_ref.nif"] = new AnimatedNif(adb, "human_male_ref.nif", "human_male.kfm", "human_male.kfb");


        List<string> nifsToBucket = new List<string>();
        foreach (entry e in lentries)
        {
            try
            {
                Model model = AnimatedModelLoader.load7305(adb, e.key);
                if (model != null)
                {
                    string nifFile = model.nifFile;

                    if (model.animated)
                    {
                        if (!nifDictionary.ContainsKey(nifFile))
                        {
                            nIFModelEntries.Add(model.displayname);
                            nifDictionary[nifFile] = new AnimatedNif(adb, nifFile, model.kfmFile, model.kfbFile);
                        }
                    }
                    else
                    {
                        // normal model
                        if (!nifDictionary.ContainsKey(nifFile))
                        {
                            nifsToBucket.Add(nifFile);
                            nifDictionary[nifFile] = new AnimatedNif(adb, nifFile, null, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to parse entry " + e.id + ":" + e.key + ":" + ex.Message);
            }
        }

        nifsToBucket.Sort();
        int bucketSize = nifsToBucket.Count / buckets.Length;
        Debug.Log("bucketsize: " + nifsToBucket.Count + ":" + bucketSize);
        foreach (string s in nifsToBucket)
        {
            for (int i = 0; i < buckets.Length; i++)
                if (buckets[i].Count <= bucketSize)
                {
                    buckets[i].Add(s);
                    break;
                }
        }


        nIFModelEntries.Sort();
        AFdropdownE.Sort();
        GLdropdownE.Sort();
        MRdropdownE.Sort();
        SZdropdownE.Sort();
        SZZdropdownE.Sort();
        SZZZdropdownE.Sort();
        AFdropdown.AddOptions(AFdropdownE);
        GLdropdown.AddOptions(GLdropdownE);
        MRdropdown.AddOptions(MRdropdownE);
        SZdropdown.AddOptions(SZdropdownE);
        SZZdropdown.AddOptions(SZZdropdownE);
        SZZZdropdown.AddOptions(SZZZdropdownE);

        nIFModelDropdown.AddOptions(nIFModelEntries);
    }
    public void toggleGround()
    {
        if (ground != null)
        {
            
            ground.SetActive(GameObject.Find("GroundToggle").GetComponent<Toggle>().isOn);
        }
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
        if (animationNif == animNif)
            return;

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
        animNif.setSkeletonRoot(nifmodel);
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

    public void changeAF()
    {
        changeNif(AFdropdown.options[AFdropdown.value].text);
    }
    public void changeGL()
    {
        changeNif(GLdropdown.options[GLdropdown.value].text);
    }
    public void changeMR()
    {
        changeNif(MRdropdown.options[MRdropdown.value].text);
    }
    public void changeSZ()
    {
        changeNif(SZdropdown.options[SZdropdown.value].text);
    }

    public void changeSZZZ()
    {
        changeNif(SZZZdropdown.options[SZZZdropdown.value].text);
    }

    public void changeSZZ()
    {
        changeNif(SZZdropdown.options[SZZdropdown.value].text);
    }

    public void changeNIF()
    {
        string nif = nIFModelDropdown.options[nIFModelDropdown.value].text;
        changeNif(nif);
    }

    // Update is called once per frame
    float tt = 0;
    bool first = false;
    void FixedUpdate()
    {
        progressText.text = progress;
        if (DBInst.loaded && !first)
        {
            first = true;
            IEnumerable<entry> entries = db.getEntriesForID(7305);
            parse7305(entries);
            changeNif("crucia.nif");
            animationNif.setActiveAnimation(animationNif.getIdleAnimIndex());
        }
        tt += animSpeed;
        if (tt > 1)
            tt = 0;
        if (animationNif != null)
            animationNif.doFrame(tt);
        //doFrame(tt);

    }


}

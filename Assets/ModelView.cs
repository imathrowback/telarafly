using Assets.NIF;
using System.Collections;
using System.Linq;
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
    int lastAnimToUse = -1;
    GameObject root;
    GameObject nifmodel;
    private AnimatedNif animationNif;
    Text progressText;
    Slider speedSlider;
    AssetDatabase adb;
    public GameObject ground;
    ImaDropdown nIFModelDropdown;
    Dropdown animationDropdown;

    Dropdown AFdropdown;
    Dropdown GLdropdown;
    Dropdown MRdropdown;
    Dropdown SZdropdown;
    Dropdown SZZdropdown;
    Dropdown SZZZdropdown;
    Dictionary<String, Model> nifDictionary = new Dictionary<string, Model>();
    DB db;
    volatile string progress = "";
    void Start()
    {
        root = GameObject.Find("ROOT");
        progressText = GameObject.Find("ProgressText").GetComponent<Text>();
        nIFModelDropdown = GameObject.Find("NIFmodelImaDropdown").GetComponent<ImaDropdown>();
        animationDropdown = GameObject.Find("AnimationDropdown").GetComponent<Dropdown>();

        /*
        AFdropdown = GameObject.Find("AFdropdown").GetComponent<Dropdown>();
        GLdropdown = GameObject.Find("GLdropdown").GetComponent<Dropdown>();
        MRdropdown = GameObject.Find("MRdropdown").GetComponent<Dropdown>();
        SZdropdown = GameObject.Find("SZdropdown").GetComponent<Dropdown>();
        SZZdropdown = GameObject.Find("SZZdropdown").GetComponent<Dropdown>();
        SZZZdropdown = GameObject.Find("SZZZdropdown").GetComponent<Dropdown>();
        */
        speedSlider = GameObject.Find("SpeedSlider").GetComponent<Slider>();
        speedSlider.value = this.animSpeed;
      
        adb = AssetDatabaseInst.DB;

        DBInst.loadOrCallback((d) => db = d);
        DBInst.progress += (m) => progress = m;
    }

    public void UseCurrentMount()
    {
        string newNifP = nIFModelDropdown.getSelected().text;
        string newNif = newNifP;
        if (newNifP.Contains(":"))
            newNif = newNifP.Split(':')[1];
        Model animNifModel = nifDictionary[newNif];
        string anim = this.animationDropdown.options[this.animationDropdown.value].text;
        if (animNifModel.mount)
        {
            Dictionary<string, string> settings = DotNet.Config.AppSettings.Retrieve("telarafly.cfg");
            settings["MOUNT_KEY"] = "" + animNifModel.key;
            settings["MOUNT_ANIM"] = anim;
            settings["MOUNT_ANIM_SPEED"] = "" + animSpeed;
            DotNet.Config.AppSettings.saveFrom(settings, "telarafly.cfg");
        }
    }
    bool mountsOnly = false;

    public void toggleShowMountsOnly(bool v)
    {
        Debug.Log("v:" + v);
        mountsOnly = v;
        /*
        this.AFdropdown.gameObject.SetActive(v);
        this.GLdropdown.gameObject.SetActive(v);
        this.MRdropdown.gameObject.SetActive(v);
        this.SZdropdown.gameObject.SetActive(v);
        this.SZZdropdown.gameObject.SetActive(v);
        this.SZZZdropdown.gameObject.SetActive(v);
        */
        if (!v)
        {
            IEnumerable<entry> entries = db.getEntriesForID(7305);
            parse7305(entries);
            changeNif("crucia.nif");
            animationNif.setActiveAnimation(animationNif.getIdleAnimIndex());
        }
        else
        {
            IEnumerable<entry> entries = db.getEntriesForID(7305);
            parse7305(entries);
        }

    }

    private void parse7305(IEnumerable<entry> entries)
    {
        

        /*this.AFdropdown.ClearOptions();
        this.GLdropdown.ClearOptions();
        this.MRdropdown.ClearOptions();
        this.SZdropdown.ClearOptions();
        this.SZZdropdown.ClearOptions();
        this.SZZZdropdown.ClearOptions();
        */
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

        nIFModelEntries.Clear();
        nifDictionary.Clear();

        List<string> nifsToBucket = new List<string>();
        foreach (entry e in lentries)
        {
            try
            {
                Model model = AnimatedModelLoader.load7305(adb, e.key);
                if (model != null)
                {
                    string nifFile = model.nifFile;
                    if (!model.mount && mountsOnly)
                        continue;
                    if (model.animated)
                    {
                        if (!nifDictionary.ContainsKey(nifFile))
                        {
                            nIFModelEntries.Add(model.displayname);
                            nifDictionary[nifFile] = model;
                                //new AnimatedNif(adb, nifFile, model.kfmFile, model.kfbFile);
                        }
                    }
                    else
                    {
                        // normal model
                        if (!nifDictionary.ContainsKey(nifFile))
                        {
                            nifsToBucket.Add(nifFile);
                            nifDictionary[nifFile] = model;
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


        //nIFModelEntries.Sort();
        /*
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
        */
        
        nIFModelDropdown.SetOptions(nIFModelEntries.Select(x => new DOption(x, null, false)));
        nIFModelDropdown.readFavs();
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
        try
        {
            Debug.Log("Change nif:" + newNifP);
            string newNif = newNifP;
            if (newNifP.Contains(":"))
                newNif = newNifP.Split(':')[1];
            Model animNifModel = nifDictionary[newNif];
            AnimatedNif animNif = gameObject.GetComponent<AnimatedNif>();
            if (animNif == null)
                animNif = gameObject.AddComponent<AnimatedNif>();
            animNif.setParams(adb, animNifModel.nifFile, animNifModel.kfmFile, animNifModel.kfbFile);

            if (nifmodel != null)
                GameObject.DestroyImmediate(nifmodel);
            Debug.Log("load nif");

            nifmodel = NIFLoader.loadNIF(animNif.nif, true);
            nifmodel.transform.parent = root.transform;

            Debug.Log("set anims dropdown");
            this.animationDropdown.ClearOptions();
            List<String> anims = new List<String>();
            foreach (KFAnimation ani in animNif.getAnimations())
            {
                anims.Add(ani.sequencename);
            }
            anims.Sort();
            Debug.Log("set skel root");
            animNif.setSkeletonRoot(nifmodel);
            animationNif = animNif;
            Debug.Log("set active anim");
            animationNif.setActiveAnimation(animationNif.getIdleAnimIndex());

            this.animationDropdown.AddOptions(anims);
            Debug.Log("DONE Change nif:" + newNifP);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }

    }
    public void changeAnim()
    {
        changeNif(nIFModelDropdown.getSelected().text);

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
        string nif = nIFModelDropdown.getSelected().text;
        changeNif(nif);
    }

    // Update is called once per frame
    bool first = false;
    void FixedUpdate()
    {
        progressText.text = progress;
        if (DBInst.loaded && !first)
        {
            nIFModelDropdown.init();
            first = true;
            IEnumerable<entry> entries = db.getEntriesForID(7305);
            parse7305(entries);
            changeNif("crucia.nif");
            animationNif.setActiveAnimation(animationNif.getIdleAnimIndex());
        }
        if (animationNif != null)
            animationNif.animSpeed = this.animSpeed;
    }


}

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
    public GameObject ground;
    Dropdown nIFModelDropdown;
    Dropdown animationDropdown;

    Dropdown AFdropdown;
    Dropdown GLdropdown;
    Dropdown MRdropdown;
    Dropdown SZdropdown;
    Dropdown SZZdropdown;
    Dropdown SZZZdropdown;
    System.Threading.Thread loadThread;
    Dictionary<String, AnimatedNif> nifDictionary = new Dictionary<string, AnimatedNif>();
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


        loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(loadDatabase));
        loadThread.Start();
    }

    void loadDatabase()
    {
        AssetEntry ae = adb.getEntryForFileName("telara.db");
        string expectedChecksum = BitConverter.ToString(ae.hash);
        db = DBInst.readDB(expectedChecksum, (s) => { progress = s; });
    }
    private string getStringMember(CObject obj, int member)
    {
        foreach (CObject o in obj.members)
            if (o.datacode == member)
                return o.convert() + "";
        return "";
    }
    private void parse(IEnumerable<entry> entries)
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
        
        List<string> nifsToBucket = new List<string>();
        foreach (entry e in lentries)
        {
            try
            {
                CObject obj = Parser.processStreamObject(new MemoryStream(e.decompressedData));
                if (obj.members.Count >= 1)
                {
                    string nif = getStringMember(obj, 2);
                    string kfm = getStringMember(obj, 1);
                    string postfix = getStringMember(obj, 33);
                    string nifFile = Path.GetFileNameWithoutExtension(nif) + ".nif";

                    if (kfm.Length > 0)
                    {
                        string kfmFile = Path.GetFileNameWithoutExtension(kfm) + ".kfm";
                        string kfbFile = Path.GetFileNameWithoutExtension(kfm) + postfix + ".kfb";
                        bool nifexists = adb.filenameExists(nifFile);
                        bool kfbexists = adb.filenameExists(kfbFile);
                        if (!(!nifexists || !kfbexists))
                        {
                            if (!nifDictionary.ContainsKey(nifFile))
                            {
                                string displayName = nifFile;
                                // special handling for mounts as we want them grouped together
                                if (postfix.Length > 0 && postfix.Contains("mount"))
                                    displayName = postfix.Replace("_", "") + ":" + nifFile;
                                nIFModelEntries.Add(displayName);
                                nifDictionary[nifFile] = new AnimatedNif(adb, nifFile, kfmFile, kfbFile);
                            }
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
                animationNif.setActiveAnimation(animationNif.getIdleAnimIndex());
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

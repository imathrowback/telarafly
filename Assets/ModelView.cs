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
using Assets.Wardrobe;

public class ModelView : MonoBehaviour
{
    public float animSpeed = 0.02f;
    public int animToUse = 0;
    int lastAnimToUse = -1;
    bool mount = false;

    GameObject root;
    GameObject character;
    Paperdoll mainPaperdoll;
    GameObject nifmodel;
    private AnimatedNif animationNif;
    Text progressText; 


    Slider speedSlider;
    AssetDatabase adb;
    public GameObject ground;
    ImaDropdown nIFModelDropdown;
    Dropdown animationDropdown;

    Dictionary<String, Model> nifDictionary = new Dictionary<string, Model>();
    DB db;
    volatile string progress = "";
    void Start()
    {
        root = GameObject.Find("ROOT");
        progressText = GameObject.Find("ProgressText").GetComponent<Text>();
        nIFModelDropdown = GameObject.Find("NIFmodelImaDropdown").GetComponent<ImaDropdown>();
        animationDropdown = GameObject.Find("AnimationDropdown").GetComponent<Dropdown>();
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
    public void toggleShowAvatar()
    {
        if (character != null)
        {
            character.SetActive(!character.activeInHierarchy);
        }

    }
    public void toggleShowMountsOnly(bool v)
    {
        mountsOnly = v;
        if (!v)
        {
            
            updateComboBoxDataModel();
            changeNif("crucia.nif");
            animationNif.setActiveAnimation(animationNif.getIdleAnimIndex());
        }
        else
        {
          
            updateComboBoxDataModel();
        }

    }

    private void updateComboBoxDataModel()
    {
        IEnumerable<entry> entries = db.getEntriesForID(7305);

        List<string> nIFModelEntries = new List<string>();
        List<entry> lentries = new List<entry>(entries);

        nIFModelEntries.Clear();
        nifDictionary.Clear();

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
        nIFModelDropdown.GetComponent<FavDropDown2>().SetOptions(nIFModelEntries.Select(x => new DOption(x, null, false)).ToList());
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
            this.mount = animNifModel.mount;
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
                Debug.Log("Found anim [" + ani.id + "]:" + ani.sequenceFilename + ":" + ani.sequencename );
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

    private void updateAvatar()
    {
        if (character == null)
            character = new GameObject();
        character.SetActive(true);
        mainPaperdoll = character.GetComponent<Paperdoll>();
        if (mainPaperdoll == null)
            mainPaperdoll = character.AddComponent<Paperdoll>();

        KFAnimation kf = animationNif.getActiveAnimation();
        string animString = kf.sequencename;
        if (kf.sequencename.StartsWith("mount"))
            animString = kf.sequencename;
        else
        {
            if (kf.sequencename.Contains("mount_"))
            {
                animString = kf.sequencename.Substring(kf.sequencename.IndexOf("mount"));
            }
        }
        Debug.Log("setting avatar animation to:" + animString);
        mainPaperdoll.animOverride = animString;
        mainPaperdoll.setKFBPostFix("mount");
        mainPaperdoll.setGender("male");
        mainPaperdoll.setRace("human");
        mainPaperdoll.animSpeed = this.animationNif.animSpeed;
        character.transform.parent = this.nifmodel.transform;
        character.transform.localPosition = new Vector3(0, 0, 0);
        character.transform.localRotation = Quaternion.identity;
        mainPaperdoll.transform.localRotation = Quaternion.identity;
        mainPaperdoll.setAppearenceSet(1044454339);
        mainPaperdoll.zeroFrame();
        this.animationNif.zeroFrame();
        
    }
    public void changeAnim()
    {
        changeNif(nIFModelDropdown.getSelected().text);

        string anim = this.animationDropdown.options[this.animationDropdown.value].text;
        animationNif.setActiveAnimation(anim);


        if (character != null)
            character.SetActive(false);
        if (mount)
        {
            updateAvatar();
        }
        if (ground != null)
            if (ground.activeInHierarchy)
                updateGround();
    }

    public void updateGround()
    {
        string anim = this.animationDropdown.options[this.animationDropdown.value].text;

        bool run = anim.Contains("run");
        bool walk = anim.Contains("walk");
        MeshRenderer mr = ground.GetComponent<MeshRenderer>();
        Material mat = mr.material;
        UVScroll uvScroll = ground.GetComponent<UVScroll>();
        uvScroll.material = mat;
        if (run || walk)
        {

            float speed = -animSpeed * 100;
            if (run)
                speed *= 2;

            if (anim.Contains("backward"))
                speed = -speed;

            uvScroll.yRate = speed;
        }
        else
        {
            uvScroll.stop();
        }
    }

    public void changeSpeed()
    {
        animSpeed = speedSlider.value;
        if (this.mainPaperdoll != null)
            mainPaperdoll.animSpeed = animSpeed;
        if (ground != null)
            updateGround();
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
          
            first = true;
           
            updateComboBoxDataModel();
            changeNif("crucia.nif");
            animationNif.setActiveAnimation(animationNif.getIdleAnimIndex());
        }
        if (animationNif != null)
            animationNif.animSpeed = this.animSpeed;
    }


}

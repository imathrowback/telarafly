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
        adb = loader.db;


        loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(loadDatabase));
        loadThread.Start();
    }

    void loadDatabase()
    {
        AssetEntry ae = adb.getEntryForFileName("telara.db");
        string expectedChecksum = BitConverter.ToString(ae.hash);
        db = DBInst.readDB(expectedChecksum, (s) => { progress = s; });
       

    }

    class AnimatedNif
    {
        Dictionary<String, GameObject> boneMap = new Dictionary<string, GameObject>();

        public string nif;
        string kfm;
        string kfb;
        AssetDatabase adb;

        KFMFile kfmfile;
        NIFFile kfbfile;
        NIFFile nifanimation;
        int activeAnimation = -1;
        List<KFAnimation> anims;
        public List<KFAnimation> getAnimations()
        {
            Debug.Log("get animations");
            if (anims != null)
                return anims;

            if (kfmfile == null)
                kfmfile = new KFMFile(new MemoryStream(adb.extractUsingFilename(kfm)));

            anims = new List<KFAnimation>();
            System.Diagnostics.Stopwatch sp = System.Diagnostics.Stopwatch.StartNew();
            sp.Start();
            foreach (KFAnimation anim in kfmfile.kfanimations)
            {
                int id = anim.id;
                //Debug.Log("[" + sp.ElapsedMilliseconds + "] check id[" + id + "]");
                if (getData(id) != null)
                {
                    anims.Add(anim);
                }
               // Debug.Log("[" + sp.ElapsedMilliseconds + "] done check id[" + id + "]");
            }

            return anims;
        }

        public AnimatedNif(AssetDatabase adb, string nif, string kfm, string kfb)
        {
            this.adb = adb;
            this.nif = nif;
            this.kfm = kfm;
            this.kfb = kfb;
        }

        private byte[] getData(int animToUse)
        {
            if (kfbfile == null)
                kfbfile = new NIFFile(new MemoryStream(adb.extractUsingFilename(this.kfb)));

            /** Choose the right animation to load from the KFB file. Ideally we should use the KFM to know what index to use */
            for (int i = 0; i < kfbfile.numObjects; i += 4)
            {
                NiIntegerExtraData indexData = (NiIntegerExtraData)kfbfile.getObject(i);
                NiIntegerExtraData sizeData = (NiIntegerExtraData)kfbfile.getObject(i + 1);
                NiBinaryExtraData binData = (NiBinaryExtraData)kfbfile.getObject(i + 2);
                NiBinaryExtraData binData2 = (NiBinaryExtraData)kfbfile.getObject(i + 3);

                int animIdx = indexData.intExtraData;
                if (animIdx == animToUse)
                    return binData.decompressed;
            }
            return null;
        }

        public NIFFile loadKFB(int animToUse)
        {
            byte[] data = getData(animToUse);
            if (data != null)
                return new NIFFile(new MemoryStream(data));
            Debug.Log("unable to load KFB for anim:" + animToUse);
            return null;
        }

        public void setActiveAnimation(string anim)
        {
            foreach (KFAnimation kfa in getAnimations())
                if (kfa.sequencename.Equals(anim))
                {
                    setActiveAnimation(kfa.id);
                    return;
                }
            Debug.Log("Unable to find animation " + anim);
        }

        public void setActiveAnimation(int anim)
        {
            Debug.Log("set anim to " + anim);
            nifanimation = loadKFB(anim);
            this.activeAnimation = anim;
            boneMap.Clear();
        }

        public void doFrame(float t)
        {
            if (nifanimation == null || activeAnimation == -1)
            {
                setActiveAnimation(0);
                if (nifanimation == null)
                {
                    return;
                }
            }

            /** For each sequence, evaluate it with the current time and apply the result to the related bone */
            foreach (NiSequenceData data in nifanimation.nifSequences)
            {
                for (int i = 0; i < data.seqEvalIDList.Count; i++)
                {
                    int evalID = (int)data.seqEvalIDList[i];
                    NiBSplineCompTransformEvaluator evalObj = (NiBSplineCompTransformEvaluator)nifanimation.getObject(evalID);
                    string boneName = nifanimation.getStringFromTable(evalObj.m_kAVObjectName);
                    GameObject go;
                    // cache game objects for bones
                    if (boneMap.ContainsKey(boneName))
                        go = boneMap[boneName];
                    else
                    {
                        go = boneMap[boneName] = GameObject.Find(boneName);
                    }
                    if (go == null)
                    {
                        //Debug.Log("unable to get gameobject for bone " + boneName);
                        continue;
                    }
                    int splineDataIndex = evalObj.splineDataIndex;
                    int basisDataIndex = evalObj.basisDataIndex;

                    if (splineDataIndex != -1 && basisDataIndex != -1)
                    {
                        NiBSplineData splineObj = (NiBSplineData)nifanimation.getObject(splineDataIndex);
                        NiBSplineBasisData basisObj = (NiBSplineBasisData)nifanimation.getObject(basisDataIndex);
                        if (evalObj.m_kRotCPHandle != 65535)
                        {
                            float[] afValues = new float[4];
                            // get the rotation values for the given time 't'
                            splineObj.getCompactValueDegree3(t, afValues, 4, basisObj, evalObj.m_kRotCPHandle,
                                evalObj.m_afCompScalars[(int)NiBSplineCompTransformEvaluator.COMP_.ROTATION_OFFSET],
                                evalObj.m_afCompScalars[(int)NiBSplineCompTransformEvaluator.COMP_.ROTATION_RANGE]);
                            // apply the rotation to the bone
                            go.transform.localRotation = new Quaternion(afValues[1], afValues[2], afValues[3], afValues[0]);
                        }
                        if (evalObj.m_kTransCPHandle != 65535)
                        {
                            // get the position values for the given time 't'
                            float[] afValues = new float[3];
                            float offset = evalObj.m_afCompScalars[(int)NiBSplineCompTransformEvaluator.COMP_.POSITION_OFFSET];
                            float range = evalObj.m_afCompScalars[(int)NiBSplineCompTransformEvaluator.COMP_.POSITION_RANGE];
                            splineObj.getCompactValueDegree3(t, afValues, 3, basisObj, evalObj.m_kTransCPHandle,
                                    offset,
                                    range);
                            go.transform.localPosition = new Vector3(afValues[0], afValues[1], afValues[2]);

                        }
                    }
                }
            }

        }

    }

    private void parse(IEnumerable<entry> entries)
    {
        nIFModelDropdown.ClearOptions();
        List<String> nIFModelEntries = new List<String>();
        foreach (entry e in entries)
        {
            CObject obj = Parser.processStreamObject(new MemoryStream(e.decompressedData));
            if (obj.members.Count > 6)
            {
                if (obj.get(0).type == 6 && obj.get(1).type == 6)
                {
                    String postfix = "";
                    String kfm = obj.get(0).convert() + "";
                    String nif = obj.get(1).convert() + "";
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
                        nIFModelEntries.Add(nifFile);
                        nifDictionary[nifFile] = new AnimatedNif(adb, nifFile, kfmFile, kfbFile);
                    }
                }
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

    public void changeNif(string newNif)
    {
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

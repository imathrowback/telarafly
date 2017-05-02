using Assets;
using Assets.DatParser;
using Assets.DB;
using Assets.NIF;
using Assets.RiftAssets;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    GameObject refModel;
    GameObject costumeParts;
    public float animSpeed = 0.02f;
    public Dropdown appearanceDropdown;
    int gender;
    int race;


    // Use this for initialization
    void Start()
    {
        root = GameObject.Find("ROOT");

        loader = new NIFLoader();
        loader.loadManifestAndDB();
        adb = loader.db;

        // defines the base model
        string nif = "human_female_refbare.nif";
        string kfm = "human_female.kfm";
        string kfb = "human_female.kfb";
        // race: 1 = human, 2 = elf, 3 = dwarf, 2005 = bahmi, 2007 = ?, 2008 = ?
        race = 1;
        gender = 2; // 0 = male, 2 = female

        this.animationNif = new Assets.AnimatedNif(adb, nif, kfm, kfb);

        GameObject go = loader.loadNIF(nif, true);
        go.transform.parent = root.transform;
        refModel = go;

        costumeParts = new GameObject("CostumeParts");
        costumeParts.transform.parent = refModel.transform;

        // always hide the boots
        enableDisableGeo("boots", go, false);


        loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(loadDatabase));
        loadThread.Start();
    }

    void process(GameObject skeleton, GameObject meshHolder, string nifFile, string geo)
    {
        NIFFile file = loader.getNIF(nifFile);
        GameObject newNifRoot = loader.loadNIF(file, nifFile, true);

        // First move all the meshes across to the skeleton

        foreach (SkinnedMeshRenderer r in newNifRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            r.transform.parent = meshHolder.transform;

        // now, process the NiSkinningMeshModifier 
        NIFLoader.linkBonesToMesh(file, skeleton);

        this.animationNif.clearBoneMap();

        // disable the proxy geo
        enableDisableGeo(nifFile, skeleton, false);

        GameObject.DestroyObject(GameObject.Find(geo));
        GameObject.DestroyObject(newNifRoot);
    }
    

    private void enableDisableGeo(string nifFile, GameObject skeleton, bool showGeo)
    {
        List<Transform> geoList = new List<Transform>();
        findChildrenContaining(skeleton.transform, "GEO", geoList);

        foreach (string s  in nifFile.Split('_'))
        {
            foreach (Transform t in geoList)
            {
                if (t.name.Contains(s + "_000_GEO") || t.name.Contains(s + "_proxy_GEO"))
                {
                    t.gameObject.SetActive(showGeo);
                    return;
                }
            }
        }

    }

   static void findChildrenContaining(Transform t, String str, List<Transform> list)
    {
        if (t.name.Contains(str))
            list.Add(t);
        foreach (Transform ct in t)
            findChildrenContaining(ct, str, list);
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
    private CObject toObj(long ds, long key)
    {
        entry e = db.getEntry(ds, key);
        MemoryStream str = new MemoryStream(e.decompressedData);
        return Parser.processStreamObject(str);
    }


    public static void SetActiveRecursively(GameObject rootObject, bool active)
    {
        rootObject.SetActive(active);

        foreach (Transform childTransform in rootObject.transform)
        {
            SetActiveRecursively(childTransform.gameObject, active);
        }
    }

    private void loadAppearenceSet(long setKey, int race, int sex)
    {
        // set the ref model to be all visible, overriden parts will be hidden later when parts are added
        SetActiveRecursively(refModel, true);
        // remove all the existing parts
        costumeParts.transform.Clear();

        Debug.Log("load appearence set[" + setKey + "] race[" + race + "] gender[" + sex + "]");
        CObject obj = toObj(7638, setKey);

        CObject setParts = obj.getMember(2);

        //process(go, "human_male_plate_foot_403.nif", "human_male_bare_foot_000_GEO");
        Debug.Log("found setparts:" + setParts.members.Count);
        foreach (CObject part in setParts.members)
        {
            int key = int.Parse(part.convert().ToString());
            CObject gearSlot = toObj(7629, key);
            int nifRefKey = gearSlot.getIntMember(2);
            CObject nifObj = toObj(7305, nifRefKey);
            Dictionary<int, CObject> dict = nifObj.getMember(5).asDict();
            CObject nifRaceObj = dict[race];
            string nif = nifRaceObj.getMember(sex).convert().ToString();
            Debug.Log("Load set part:" + nif);
            process(refModel, costumeParts, Path.GetFileName(nif), "");
        }
        //Debug.Log("loading")
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

                // finally everything is loaded and ready so lets load an appearence set
                try
                {
                   
                    //                    loadAppearenceSet(-89842968, 1, 2);
                    //loadAppearenceSet(2128251532, 1, 2);

                    appearanceDropdown.ClearOptions();
                    List<DOption> options = new List<DOption>();
                    foreach (entry e in db.getEntriesForID(7638))
                    {
                        CObject _7637 = toObj(e.id, e.key);
                        string str = _7637.getMember(0).convert().ToString();
                        DOption option = new DOption();
                        option.text = str;
                        option.userObject = e;
                        options.Add(option);
                    }

                    appearanceDropdown.AddOptions(options.Cast<Dropdown.OptionData>().ToList());
                    
                }
                catch (Exception ex)
                {
                    Debug.Log("failed to load appearence set: " + ex);
                }
            }
        }
    }

    public void changeAppearance()
    {
        int v = appearanceDropdown.value;
        DOption option = (DOption)appearanceDropdown.options[v];
        entry entry =(entry) option.userObject;
        loadAppearenceSet(entry.key, race, gender);
    }

    float tt = 0;
    private AnimatedNif animationNif;

    void FixedUpdate()
    {
        tt += animSpeed;
        if (tt > 1)
            tt = 0;
        if (animationNif != null)
        {
            animationNif.doFrame(tt);
           
        }
    }

    class DOption : Dropdown.OptionData
    {
        public object userObject { get; set; }
    }
}

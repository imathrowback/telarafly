using Assets.Database;
using Assets.DatParser;
using Assets.NIF;
using Assets.RiftAssets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Wardrobe
{
    public delegate void OnFinished();
    public delegate void ThreadedCall();

    public class WardrobeNIFLoadJob : ThreadedJob
    {
        NIFLoader loader;
        public NIFFile niffile;
        public event OnFinished onFinished = delegate { };
        public event ThreadedCall onThreadFunc = delegate { };
        public AnimatedNif animationNif;
        public WardrobeNIFLoadJob()
        {
    
        }
        protected override void ThreadFunction()
        {
            // Do your threaded task. DON'T use the Unity API here
            
            onThreadFunc.Invoke();
        }

        protected override void OnFinished()
        {
            // This is executed by the Unity main thread when the job is finished
            onFinished.Invoke();
        }
    }
    

    public class Paperdoll : MonoBehaviour
    {
        GameObject refModel;
        GameObject costumeParts;
        AnimatedNif animationNif;
        string raceString = "human";
        string genderString = "male";
        public float animSpeed = 0.01f;
        NIFLoader loader;
        AssetDatabase adb;
        DB db;
        WardrobeNIFLoadJob nifJobLoad;

        public string getGenderString()
        {
            return genderString;
        }
        public string getRaceString()
        {
            return raceString;
        }
        void Start()
        {
            init();
        }

        public void init()
        {
            if (loader != null)
                return;
            loader = new NIFLoader();
            loader.loadManifestAndDB();
            adb = loader.db;
            DBInst.loadOrCallback((d) => db = d);
            updateRaceGender();
        }

        string getBaseModel()
        {
            return string.Format("{0}_{1}", raceString, genderString);
        }

        public void updateRaceGender()
        {
            // ensure the paperDoll is initialized
            init();

            
            if (refModel != null)
                GameObject.DestroyImmediate(refModel);
            if (costumeParts != null)
                GameObject.DestroyImmediate(costumeParts);

            // defines the base model
            string nif = string.Format("{0}_refbare.nif", getBaseModel());
            string kfm = string.Format("{0}.kfm", getBaseModel());
            string kfb = string.Format("{0}.kfb", getBaseModel());

            
            animationNif = new Assets.AnimatedNif(adb, nif, kfm, kfb);

            NIFFile file = loader.getNIF(nif);
            GameObject go = loader.loadNIF(file, nif, true);
            go.transform.parent = this.transform;
            go.transform.localPosition = Vector3.zero;
            refModel = go;


            animationNif.setActiveAnimation(string.Format("{0}_unarmed_idle", getBaseModel()));
            animationNif.setSkeletonRoot(refModel);
                
            costumeParts = new GameObject("CostumeParts");
            costumeParts.transform.parent = refModel.transform;

            // always hide the boots
            enableDisableGeo("boots", go, false);

            // if the model is female, give it some clothes for "modesty" because for some reason all female models except bahmi are "nude"
            if (genderString.Equals("female"))
            {
                if (db != null)
                {
                    if (!gearSlots.ContainsKey(GearSlot.TORSO))
                        setGear(GearSlot.TORSO, 1127855431);
                    if (!gearSlots.ContainsKey(GearSlot.LEGS))
                        setGear(GearSlot.LEGS, 1300181064);
                }
            }

            //this.animationNif = nifJobLoad.animationNif;


        }

        void loadDefault()
        {
            // set default gear
            int race = WardrobeStuff.raceMap[raceString];
            int sex = WardrobeStuff.genderMap[genderString];
            loadAppearenceSet(176073892, race, sex);
        }

        Dictionary<GearSlot, GameObject> gearSlots = new Dictionary<GearSlot, GameObject>();
        public void setGear(GearSlot slot, long key)
        {
            if (nifJobLoad != null)
                while (!nifJobLoad.IsDone) ;
            int race = WardrobeStuff.raceMap[raceString];
            int sex = WardrobeStuff.genderMap[genderString];
            ClothingItem item = new ClothingItem(db, key);
            string nif = item.nifRef.getNif(race, sex);

            if (gearSlots.ContainsKey(slot))
                GameObject.DestroyImmediate(gearSlots[slot]);

            gearSlots[slot] = loadNIFForSlot(slot, refModel, costumeParts, Path.GetFileName(nif), "");
        }
        public void loadAppearenceSet(long setKey, int race, int sex)
        {
            // set the ref model to be all visible, overriden parts will be hidden later when parts are added
            SetActiveRecursively(refModel, true);
            // remove all the existing parts
            costumeParts.transform.Clear();

            CObject obj = db.toObj(7638, setKey);
            CObject setParts = obj.getMember(2);

            foreach (CObject part in setParts.members)
            {
                ClothingItem item = new ClothingItem(db, int.Parse(part.convert().ToString()));
                setGear(item.allowedSlots.First(), item.key);
            }
        }

      

        private GameObject loadNIFForSlot(GearSlot slot, GameObject skeleton, GameObject meshHolder, string nifFile, string geo)
        {
            // First move all the meshes across to the skeleton
            GameObject meshes = new GameObject(slot.ToString());
            try
            {
                NIFFile file = loader.getNIF(nifFile);
                GameObject newNifRoot = loader.loadNIF(file, nifFile, true);

                meshes.transform.parent = meshHolder.transform;

                foreach (SkinnedMeshRenderer r in newNifRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    r.transform.parent = meshes.transform;

                // weapons are a bit different
                if (!WardrobeStuff.isWeapon(slot))
                    // process the NiSkinningMeshModifier 
                    NIFLoader.linkBonesToMesh(file, skeleton);
                else
                {
                    /*
                    switch (slot)
                    {
                        case GearSlot.MAIN_HAND:
                        */
                            //newNifRoot.FindDeepChild("propShape").localPosition = Vector3.zero;
                            Transform t = skeleton.transform.FindDeepChild("AP_r_hand");
                            meshes.transform.parent = t;
                            meshes.transform.localPosition = new Vector3(0, 0, 0);
                            //meshes.transform.localPosition = new Vector3(0, 0, 0);
                            //meshes.transform.GetChild(0).localPosition = Vector3.zero;

                    /*
                            break;
                        default:
                            break;

                    }
                    */
                }

                this.animationNif.clearBoneMap();

                // disable the proxy geo
                enableDisableGeo(nifFile, skeleton, false);
                // special case to ensure boots are disabled as well
                if (nifFile.Contains("foot"))
                    enableDisableGeo("boots", skeleton, false);

                //GameObject.DestroyObject(GameObject.Find(geo));
                GameObject.DestroyObject(newNifRoot);
            }
            catch (Exception ex)
            {
                Debug.Log("Exception trying to load nif[" + nifFile + "]" + ex);
            }
            return meshes;
        }

        public void setRace(string race)
        {
            this.raceString = race;
        }

        public void setGender(string gender)
        {
            this.genderString = gender;
        }

        float tt = 0;
       
        public void Update()
        {
            if (nifJobLoad != null)
                nifJobLoad.Update();
           
        }

        public void FixedUpdate()
        {
            tt += animSpeed;
            if (tt > 1)
                tt = 0;
            if (animationNif != null)
            {
                //Debug.Log("animate[" + this.GetInstanceID() + "] nif:" + animationNif);
                animationNif.doFrame(tt);

            }
        }


        public static void SetActiveRecursively(GameObject rootObject, bool active)
        {
            rootObject.SetActive(active);

            foreach (Transform childTransform in rootObject.transform)
            {
                SetActiveRecursively(childTransform.gameObject, active);
            }
        }

        static void findChildrenContaining(Transform t, String str, List<Transform> list)
        {
            if (t.name.Contains(str))
                list.Add(t);
            foreach (Transform ct in t)
                findChildrenContaining(ct, str, list);
        }

        private void enableDisableGeo(string nifFile, GameObject skeleton, bool showGeo)
        {
            List<Transform> geoList = new List<Transform>();
            findChildrenContaining(skeleton.transform, "GEO", geoList);

            foreach (string s in nifFile.Split('_'))
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
    }
}

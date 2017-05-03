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
    public class Paperdoll : MonoBehaviour
    {
        GameObject refModel;
        GameObject costumeParts;

        string raceString = "human";
        string genderString = "male";
        public float animSpeed = 0.01f;
        NIFLoader loader;
        AssetDatabase adb;
        DB db;

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
            if (!DBInst.loaded)
            {
                DBInst.loadedCallback += (d) =>
                {
                    db = d;
                    //updateRaceGender();
                };
            }
            else db = DBInst.inst;
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


            GameObject go = loader.loadNIF(nif, true);
            go.transform.parent = this.transform;
            go.transform.localPosition = Vector3.zero;
            refModel = go;

            this.animationNif = new Assets.AnimatedNif(adb, nif, kfm, kfb);
            this.animationNif.setActiveAnimation(string.Format("{0}_unarmed_idle", getBaseModel()));
            this.animationNif.setSkeletonRoot(refModel);

            costumeParts = new GameObject("CostumeParts");
            costumeParts.transform.parent = refModel.transform;

            // always hide the boots
            enableDisableGeo("boots", go, false);
        }
        Dictionary<GearSlot, GameObject> gearSlots = new Dictionary<GearSlot, GameObject>();
        public void setGear(GearSlot slot, long key)
        {
            int race = WardrobeStuff.raceMap[raceString];
            int sex = WardrobeStuff.genderMap[genderString];
            ClothingItem item = new ClothingItem(db, key);
            string nif = item.nifRef.getNif(race, sex);

            if (gearSlots.ContainsKey(slot))
                GameObject.Destroy(gearSlots[slot]);

            gearSlots[slot] = process(refModel, costumeParts, Path.GetFileName(nif), "");
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

        GameObject process(GameObject skeleton, GameObject meshHolder, string nifFile, string geo)
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
            // special case to ensure boots are disabled as well
            if (nifFile.Contains("foot"))
                enableDisableGeo("boots", skeleton, false);

            //GameObject.DestroyObject(GameObject.Find(geo));
            //GameObject.DestroyObject(newNifRoot);
            return newNifRoot;
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
        private AnimatedNif animationNif;

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
    }
}

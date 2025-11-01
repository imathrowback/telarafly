using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Assets.DatParser;
using Assets.RiftAssets;
using Assets.Database;
using UnityEngine;

namespace Assets
{
    public class Model
    {
        public string nifFile;
        public string kfmFile;
        public string kfbFile;
        public long key;
        public bool animated = false;

        public string displayname;
        public bool mount = false;
    }


    public class AnimatedModelLoader
    {
        static public GameObject loadNIF(string modelName)
        {
            IEnumerable<entry> entries = DBInst.inst.getEntriesForID(7305);
            foreach(entry entry in entries)
            {
                try
                {
                    long key = entry.key;
                    Model model = load7305(AssetDatabaseInst.DB, key);
                    if (model.nifFile.Equals(modelName))
                    {
                        Debug.Log("search [" + modelName + "] found key:" + key);
                        return loadNIF(key);
                    }
                }catch(Exception ex)
                {

                }
            }
            return null;
        }

        static public GameObject loadNIF(long key)
        {
            Model model = load7305(AssetDatabaseInst.DB, key);
            GameObject nifmodel = NIFLoader.loadNIF(model.nifFile, true);
            AnimatedNif nif = nifmodel.AddComponent<AnimatedNif>();
            nif.setParams(AssetDatabaseInst.DB, model.nifFile, model.kfmFile, model.kfbFile);
            nif.setSkeletonRoot(nifmodel);
            return nifmodel;
        }
        static public GameObject loadNIFFromFile(string nifFile, string kfm, string kfb)
        {
            GameObject nifmodel = NIFLoader.loadNIFFromFile(nifFile, true);
            AnimatedNif nif = nifmodel.AddComponent<AnimatedNif>();
            nif.setParams(AssetDatabaseInst.DB, nifFile, kfm, kfb);
            nif.setSkeletonRoot(nifmodel);
            return nifmodel;
        }
        static public string getStringMember(CObject obj, int member)
        {
            foreach (CObject o in obj.members)
                if (o.datacode == member)
                    return o.convert() + "";
            return "";
        }
        static public Model load7305(AssetDatabase adb, long key)
        {
            //Debug.Log("load 7305:" + key);
            entry e = DBInst.inst.getEntry(7305, key);
            Model model = null;
            CObject obj = Parser.processStreamObject(new MemoryStream(e.decompressedData));
            if (obj.members.Count >= 1)
            {
                string dual = getStringMember(obj, 33);
                bool isDual = dual.Contains("_dual");
                //Debug.Log("dual " + dual);
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
                    if (nifexists)
                    {
                        string displayName = nifFile;
                        // special handling for mounts as we want them grouped together
                        model = new Model();
                        if (postfix.Length > 0 && postfix.Contains("mount") || isDual)
                        {
                            displayName = postfix.Replace("_", "") + ":" + nifFile;
                            model.mount = true;
                        }
                        model.animated = true;
                        model.nifFile = nifFile;
                        model.kfmFile = kfmFile;
                        model.kfbFile = kfbFile;
                        model.key = key;
                        model.displayname = displayName;


                    }
                }
                else
                {
                    model = new Model();

                    model.animated = false;
                    model.nifFile = nifFile;
                    model.kfmFile = null;
                    model.kfbFile = null;
                    model.key = key;
                    model.displayname = nifFile; ;
                }
            }
            return model;
        }
    }
}

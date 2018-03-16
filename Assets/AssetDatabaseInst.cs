using Assets.RiftAssets;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets
{
    public class AssetDatabaseInst
    {
        private static AssetDatabaseInst inst;
        public static AssetDatabaseInst Inst { get { return getInst(); } }
        public static AssetDatabase DB { get { return getInst().getDB(); } }
        //public static Manifest Manifest { get { return getInst().getManifest(); } }
        //public static string ManifestFile {  get { return getInst().assetsManifest; } }
        public static string AssetsDirectory { get { return getInst().assetsDirectory; } }
        private Manifest manifest;
        private AssetDatabase db;
        private string assetsManifest;
        private string assetsDirectory;
        private string overrideDirectory;
        static object lockObj = new object();
        private void init()
        {
            overrideDirectory = ProgramSettings.get("ASSETS_OVERRIDE_DIR");
            assetsDirectory = ProgramSettings.get("ASSETS_DIR");
            if (assetsDirectory == null)
                throw new Exception("Assets directory was null");
            assetsManifest = ProgramSettings.get("ASSETS_MANIFEST");
            if (assetsManifest == null)
                throw new Exception("Assets manifest was null");


            if (overrideDirectory != null)
            {
                Debug.Log("Detected override directory, checking for validity");
                // check the override directory for a manifest
                string manifestName = Path.GetFileName(assetsManifest);
                string overriddenManifest = overrideDirectory + Path.DirectorySeparatorChar + manifestName;
                Debug.Log("checking for overriden manifest:" + overriddenManifest);
                if (File.Exists(overriddenManifest))
                {
                    assetsManifest = overriddenManifest;
                    Debug.Log("Found overriden manifest");
                }
                else
                    Debug.Log("No valid overide manifest found");
            }

            manifest = new Manifest(assetsManifest);
            db = AssetProcessor.buildDatabase(manifest, assetsDirectory, overrideDirectory);
        }

        public AssetDatabase getDB()
        {
            return db;
        }

        /*
        public Manifest getManifest()
        {
            return manifest;
        }
        */

        public static AssetDatabaseInst getInst()
        {
            lock (lockObj)
            {

                if (inst == null)
                {
                    inst = new AssetDatabaseInst();
                    inst.init();
                }
                return inst;
            }
        }


    }
}

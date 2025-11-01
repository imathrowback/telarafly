using Assets.RiftAssets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        
        private bool initializeRemote()
        {
            string remoteUrl = ProgramSettings.get("REMOTE_ASSETS_URL", "") + "recovery64/assets64.manifest";
            if (remoteUrl == null || remoteUrl.Length == 0)
            {
                Debug.LogWarning("Remote assets URL not set, using local assets instead.");
                return false;
            }

            Debug.Log("Attemping to initialize remote assets database with URL: " + remoteUrl);
            string cacheDir = Path.GetTempPath() + "rift/";
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            string cacheFile = Path.GetTempPath() + "rift/" + Util.hashFileName(remoteUrl);
            if (File.Exists(cacheFile))
            {
                Debug.Log("Using cached manifest at " + cacheFile);
                manifest = new Manifest(File.ReadAllBytes(cacheFile), true);
            }
            else
            {
                Debug.Log("No cached manifest found, downloading from remote server: " + remoteUrl);
                byte[] manifestData = new HttpClient().GetByteArrayAsync(remoteUrl).Result;
                Debug.Log("Caching..");
                File.WriteAllBytes(cacheFile, manifestData);
                manifest = new Manifest(manifestData, true);

            }

            db = new AssetDatabaseRemote(manifest);
            return true;
        }

        private void init()
        {
            if (ProgramSettings.get("USE_REMOTE_ASSETS", "true").ToLower() == "true")
            {
               try
                {
                    if (initializeRemote())
                        return;
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to initialize remote assets: " + e.Message);
                }
                Debug.LogWarning("Remote assets setup failed, using local assets instead.");
            }

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
            db = LocalAssetProcessor.buildDatabase(manifest, assetsDirectory, overrideDirectory);

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

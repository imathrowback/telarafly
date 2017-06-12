using Assets.RiftAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets
{
    public class AssetDatabaseInst
    {
        private static AssetDatabaseInst inst;
        public static AssetDatabaseInst Inst { get { return getInst(); } }
        public static AssetDatabase DB { get { return getInst().getDB(); } }
        public static Manifest Manifest { get { return getInst().getManifest(); } }
        public static string ManifestFile {  get { return getInst().assetsManifest; } }
        public static string AssetsDirectory { get { return getInst().assetsDirectory; } }
        private Manifest manifest;
        private AssetDatabase db;
        private String assetsManifest = "L:\\SteamStuff\\Steam2\\steamapps\\common\\rift\\assets64.manifest";
        private String assetsDirectory = "L:\\SteamStuff\\Steam2\\steamapps\\common\\rift\\assets\\";
        static object lockObj = new object();
        private void init()
        {
            Properties p = new Properties("nif2obj.properties");
            assetsDirectory = (p.get("ASSETS_DIR"));
            assetsManifest = (p.get("ASSETS_MANIFEST"));
            manifest = new Manifest(assetsManifest);
            db = AssetProcessor.buildDatabase(manifest, assetsDirectory);
        }

        public AssetDatabase getDB()
        {
            return db;
        }

        public Manifest getManifest()
        {
            return manifest;
        }

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

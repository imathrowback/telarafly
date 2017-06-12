using Assets.RiftAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets
{
    public class AssetDatabaseInst
    {
        public delegate void ProgressD(string s);
        public static ProgressD progressUpdate = delegate { };

        private static AssetDatabaseInst inst;
        public static AssetDatabaseInst Inst { get { return getInst(); } }
        public static AssetDatabase DB { get { return getInst().getDB(); } }
        public static Manifest Manifest { get { return getInst().getManifest(); } }
        //private static string ManifestFile {  get { return getInst().assetsManifest; } }
        //private static string AssetsDirectory { get { return getInst().assetsDirectory; } }
        private Manifest manifest;
        private AssetDatabase db;
        private String assetsManifest = "L:\\SteamStuff\\Steam2\\steamapps\\common\\rift\\assets64.manifest";
        private String assetsDirectory = "L:\\SteamStuff\\Steam2\\steamapps\\common\\rift\\assets\\";
        bool local = false;

        private void init()
        {
            if (local)
            {
                Properties p = new Properties("nif2obj.properties");
                assetsDirectory = (p.get("ASSETS_DIR"));
                assetsManifest = (p.get("ASSETS_MANIFEST"));
                db = LocalAssetProcessor.buildDatabase(new Manifest(assetsManifest), assetsDirectory);
            }
            else
            {
                RemoteAssetDatabase rad = new RemoteAssetDatabase();
                rad.progressUpdate += (s) =>  progressUpdate.Invoke(s);
                db = rad;
            }
            manifest = db.getManifest();
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
            if (inst == null)
            {
                inst = new AssetDatabaseInst();
                inst.init();
            }
            return inst;
        }


    }
}

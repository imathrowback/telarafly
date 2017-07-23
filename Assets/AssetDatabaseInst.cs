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
        private string assetsManifest;
        private string assetsDirectory;
        static object lockObj = new object();
        private void init()
        {
            
            assetsDirectory = ProgramSettings.get("ASSETS_DIR");
            if (assetsDirectory == null)
                throw new Exception("Assets directory was null");
            assetsManifest = ProgramSettings.get("ASSETS_MANIFEST");
            if (assetsManifest == null)
                throw new Exception("Assets manifest was null");
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

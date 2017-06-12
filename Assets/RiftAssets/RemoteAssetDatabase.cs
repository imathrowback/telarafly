using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assets.RiftAssets
{
    public class RemoteAssetDatabase : AssetDatabase
    {
        public delegate void ProgressD(string s);
        public ProgressD progressUpdate = delegate { };

        int lang = 1;
        RemotePAK rPak;
        Manifest manifest;
        public RemoteAssetDatabase()
        {
            rPak = new RemotePAK(RemoteType.LIVE);
            rPak.progressUpdate += (s) =>  progressUpdate.Invoke(s);
        }

        override public Manifest getManifest()
        {
            if (manifest == null)
            {
                // download the remote manifest
                byte[] manifestData = rPak.downloadManifest();
                manifest = new Manifest(manifestData, true);
            }
            return manifest;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override byte[] extractUsingFilename(string filename)
        {
            string hash = Util.hashFileName(filename);
            
            foreach (ManifestEntry me in manifest.manifestEntries)
            {
                if (me.lang == 0 || me.lang == 1)
                    if (me.hashStr.Equals(hash))
                        return rPak.download(manifest, me);
            }
            return null;
        }

        public override bool filenameExists(string filename)
        {
            string hash = Util.hashFileName(filename);

            foreach (ManifestEntry me in manifest.manifestEntries)
            {
                if (me.lang == 0 || me.lang == 1)
                    if (me.hashStr.Equals(hash))
                        return true;
            }
            return false;
        }

        public override string getHash(string filename)
        {
            string hash = Util.hashFileName(filename);

            foreach (ManifestEntry me in manifest.manifestEntries)
            {
                if (me.lang == 0 || me.lang == 1)
                    if (me.hashStr.Equals(hash))
                        return me.shaStr; ;
            }
            return null;
        }
    }
}

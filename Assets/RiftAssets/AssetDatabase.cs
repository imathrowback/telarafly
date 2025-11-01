using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace Assets.RiftAssets
{
    public abstract class AssetDatabase
    {
       

        Manifest _manifest;

        public abstract bool isRemote();
        

        public AssetDatabase(Manifest manifest)
        {
            this._manifest = manifest;
        }

        public Manifest getManifest()
        {
            return _manifest;
        }

        public Manifest manifest()
        {
            return _manifest;
        }


        public bool is64()
        {
            return _manifest.getIs64();
        }


        public abstract byte[] extractUsingFilename(string filename, RequestCategory requestCategory = RequestCategory.NONE);

        public bool filenameExists(string filename)
        {
            return _manifest.containsHash(Util.hashFileName(filename));
        }
       
        protected String getID(string filename, RequestCategory requestCategory = RequestCategory.NONE)
        {
            //Debug.Log("get entry for filename:" + filename + " with request category " + requestCategory);
            List<ManifestEntry> entries = manifest().getEntriesForFilenameHash(Util.hashFileName(filename));

            if (entries.Count() == 0)
            {
                // lets see if the filename is actually a hash (this shouldn't happen, but whatevers)
                entries = manifest().getEntriesForFilenameHash(filename);
                if (entries.Count() == 0)
                    throw new Exception("Filename hash not found in manifest: '" + filename + "'");
                //Debug.LogWarning("Using filename[" + filename + "] as hash");
            }

            //Debug.Log("found " + entries.Count() + " entries in manifest that match, strip out duplicate patch paks");
            // strip out duplicate patch paks
            entries.RemoveAll(e => {
                return manifest().getPAKName(e.pakIndex).Contains("patch") && entries.Any(x => x != e && x.idStr.Equals(e.idStr));
            });

            //Debug.Log("found " + entries.Count() + " entries in manifest that match");
            string id = "";
            if (entries.Count() == 1)
            {
                // if there was only one result, then use it
                ManifestEntry firstEntry = entries.First(); 
                id = firstEntry.idStr;
                /*Debug.Log("Got idStr:" + id);
                if (!Util.hashFileName(filename).Equals(entries.First().filenameHashStr, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError("[A]: Filename hash does not match entry hash for " + filename + ". Got: " + entries.First().filenameHashStr + ", expected: " + Util.hashFileName(filename));
                }
                ManifestEntry entry = manifest().getEntry(id);
                if (!Util.hashFileName(filename).Equals(entry.filenameHashStr, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError("[B]: Filename hash does not match entry hash for " + filename + ". Got: " + entry.filenameHashStr + ", expected: " + Util.hashFileName(filename));
                    Debug.Log("firstEntry" + firstEntry);
                    Debug.Log("SecondEntry" + entry);
                }*/
            }
            else
            { 

                ManifestEntry finalEntry = null;

                // work out which one we want based on the category
                string requestStr = requestCategory.ToString().ToLower();
                Debug.Log("multiple ids found for " + filename + ", using request category " + requestStr);

                foreach (ManifestEntry entry in entries)
                {
                    Debug.Log("[" + filename + "]: considering entry:" + entry + " :" + manifest().getPAKName(entry.pakIndex));
                    ManifestPAKFileEntry pak = manifest().getPAK(entry.pakIndex);
                    string pakName = pak.name;
                    if (pakName.Contains(requestStr))
                    {
                        finalEntry = entry;
                        break;
                    }
                }


                if (finalEntry == null)
                {
                    // if we were still unable to break the tie
                    Debug.LogWarning("tiebreak for " + filename + " no id match");

                    // one final check on the language, if an english one exists, use that over any other non-english one
                    IEnumerable<ManifestEntry> engUni = entries.Where(e => e.lang == 0 || e.lang == 1);
                    // if the number of english entries is different to the number of entries, then we should choose an english one and assume it is that one
                    if (engUni.Count() > 0 && engUni.Count() != entries.Count())
                    {
                        Debug.Log("tie broken with english language choice: " + finalEntry + " :" + manifest().getPAKName(finalEntry.pakIndex));
                        finalEntry = engUni.First();
                    }
                    else
                    {
                        // fail?
                        String str = "";
                        foreach (ManifestEntry entry in entries)
                        {
                            str += "\t" + entry + " :" + manifest().getPAKName(entry.pakIndex) + "\n";
                        }
                        string errStr = ("Multiple ids match the filename [" + filename + "] but no request category was given, unable to determine which to return, picking one!!\n" + str);
                        Debug.LogWarning(errStr);
                        finalEntry = engUni.First();
                        //throw new Exception(errStr);
                    }
                }
                id = finalEntry.idStr;
                Debug.Log("settled on entry:" + finalEntry + " :" + manifest().getPAKName(finalEntry.pakIndex));

            }
            return id;
        }


        public enum RequestCategory
        {
            NONE,
            MAP,
            PHYSICS,
            TEXTURE,
            SHADER,
            SHADER_FORWARD,
            GEOMETRY,
            CHARACTER,
            PARTICLE,
            VFX,
            UIFONT,
            UIFLASH,
            MOVIE,
            AUDIO,
            PROPERTYCLASSDATA,
            GAMEDATA,
            ENGLISH,
            PATCH,
        }
    }

}

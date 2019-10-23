using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Database;
using Assets.RiftAssets;
using Assets;
using Assets.NIF;
using System.Linq;
using Assets.Wardrobe;
using Assets.Export;
using System.IO;
using System;
using UnityEngine.UI;
public class obj_export_all : MonoBehaviour {
    public Text text;
    OBJExport exporter = new OBJExport();
    AssetDatabase adb;
    DB db;
    
    
    // Use this for initialization
    void Start () {
        adb = AssetDatabaseInst.DB;
        DBInst.loadOrCallback((d) => db = d);
    }
    GameObject go = null;
    Coroutine wRoutine;
	// Update is called once per frame
	void Update () {
        if (db != null && go == null && wRoutine == null)
        {
            wRoutine = StartCoroutine(doWardrobe());
        }
        NIFTexturePool.inst.process();
        
    }

    int ecount = 0;
    IEnumerator doWardrobe()
    {
        
        Debug.Log("process");
       
        List<ClothingItem> items = db.getClothing().ToList();
        Debug.Log("found " + items.Count + " original items to process");

        ExportModelData.langIDs.Add(1748235087);
        if (ExportModelData.langIDs.Count > 0)
        {
            Debug.Log("Found filter for " + ExportModelData.langIDs.Count + " items");
            items = items.Where(ci => ExportModelData.langIDs.Contains(ci.langKey)).ToList();
            Debug.Log("filtered to only " + items.Count + " items");
        }


        ecount = 0;
        text.text = ecount + "/" + items.Count();
            
        foreach (ClothingItem item in items)
        {
            string appName = DBInst.lang_inst.get(item.langKey);
            string appKey = "" + item.langKey;

            foreach (string racestr in WardrobeStuff.raceMap.Keys)
            {
                int race = WardrobeStuff.raceMap[racestr];

                foreach (string genderstr in WardrobeStuff.genderMap.Keys)
                {
                    try
                    {
                        int gender = WardrobeStuff.genderMap[genderstr];
                        string nifname = item.nifRef.getNif(race, gender);

                        string fname = appKey + "_" + racestr + "_" + genderstr + "";
                        string itemName = appKey + "_" + racestr + "_" + genderstr + "";

                        List<string> additionalComments = new List<string>();
                        additionalComments.Add(nifname);
                        additionalComments.Add(appName);

                        if (go != null)
                            Destroy(go);
                        go = NIFLoader.loadNIF(Path.GetFileName(nifname));
                        NIFTexturePool.inst.process();
                        exporter.export(go, ExportModelData.outputDirectory, fname, additionalComments);
                    }
                    catch (Exception ex)
                    {
                        
                        Debug.LogWarning(ex.Message + ":" + ex.StackTrace);
                    }
                    yield return new WaitForEndOfFrame();
                }
            }
            text.text = ecount++ + "/" + items.Count();
            //if (ecount > 20)
            //    yield break;
        }

        Debug.Log("quit, did " + ecount + " objects");
        // save any game data here
#if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif

        /*
       
        ecount = 0;
        using (StreamWriter sw = new StreamWriter(@"l:\rift\items_db.csv"))
        {
            sw.WriteLine("db_id\tdb_key\thidden\tinternal name\ttext\tmaterial type\tvalid slots\ticon");
            foreach (ClothingItem item in items)
            {
                string slotsStr = String.Join(",", item.allowedSlots.Select(x => x.ToString()).ToArray());
                string iconStr = item.icon;
                
                string output = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", new object[] { item.id, item.key, item.hidden ? "1" : "0", item.name,DBInst.lang_inst.get(item.langKey), item.type, slotsStr, iconStr }) ;
                sw.WriteLine(output );
                text.text = ecount++ + "/" + items.Count();
                yield return new WaitForEndOfFrame();
            }
        }
        
        
        using (StreamWriter sw = new StreamWriter(rootdir + "\\" + dir + "\\index"))
        {
            index.ToList().ForEach(x => sw.WriteLine(x.Key + ":" + x.Value));
        }
        */
        yield return null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets.Export;
using Assets.RiftAssets;
using Assets;
using System.IO;

public class TestNifLoader : MonoBehaviour {
    GameObject go;
    // Use this for initialization
    void Start () {

        DBLang lang = new DBLang(AssetDatabaseInst.DB, "english", null);
        using (FileStream fs = new FileStream("out.lang", FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                foreach (int i in lang.keys)
                {
                    writer.WriteLine(i + ":" + lang.get(i));
                }
            }
        }

            /*
        NIFLoader loader = new NIFLoader();

        go = loader.loadNIF("world_terrain_6144_5120_split.nif");
        go.transform.parent = this.transform;

        string input = "output.obj";

        OBJExport exporter = new OBJExport();
        exporter.export(go, input);
        applyLOD(go);
        */
    }
    

   
    private void applyLOD(GameObject go)
    {
        Renderer [] renderers = go.GetComponentsInChildren<Renderer>();
        LODGroup group = go.AddComponent<LODGroup>();
        group.animateCrossFading = true;
        group.fadeMode = LODFadeMode.SpeedTree;
        LOD[] lods = new LOD[2];
        lods[0] = new LOD(0.9f, renderers);
        lods[1] = new LOD(0.1f, renderers);
        group.SetLODs(lods);


    }
	
	// Update is called once per frame
	void Update () {
		
	}
}

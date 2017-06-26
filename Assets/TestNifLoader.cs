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

        byte[] key = { 0x22, 0x8A, 0x28, 0x5B, 0x7C, 0xEC, 0x42, 0x09, 0xB6, 0xD9, 0x76, 0x95, 0x43, 0x46, 0x0E, 0x34, 0x02, 0x9E, 0x84, 0xFC, 0x89, 0xA8, 0x4C, 0x9A, 0xA1, 0x0E, 0xEC, 0x12, 0xA7, 0xF5, 0x5C, 0x37 };
        Debug.Log(Util.bytesToHexString(key));

        Debug.Log(System.Convert.ToBase64String(key));
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

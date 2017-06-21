using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestNifLoader : MonoBehaviour {

	// Use this for initialization
	void Start () {
        NIFLoader loader = new NIFLoader();
        GameObject go = loader.loadNIF("human_female_refbare.nif");
        go.transform.parent = this.transform;
        applyLOD(go);
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

using Assets;
using Assets.RiftAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class model_trans_test : MonoBehaviour {

    // Use this for initialization
    void Start () {



        GameObject go = NIFLoader.loadNIF("N_SF_rock_water_rapids_01.nif");
        go.transform.parent = this.transform;
        /*
        string x = "A_TNB_embassy_01.nif";

        go = load.loadNIF(x);
        go.transform.parent = this.transform;

        x = "N_EC_tree_07.nif";
        go = load.loadNIF(x);
        go.transform.parent = this.transform;
        */
    }

    // Update is called once per frame
    void Update () {
		
	}
}

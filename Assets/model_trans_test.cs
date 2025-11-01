using Assets;
using Assets.NIF;
using Assets.RiftAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class model_trans_test : MonoBehaviour {

    // Use this for initialization
    void Start () {

        byte[] data = AssetDatabaseInst.DB.extractUsingFilename("P_F_gloamwood_lamp_post_02.nif", AssetDatabase.RequestCategory.NONE);
        File.WriteAllBytes("P_F_gloamwood_lamp_post_02.nif", data);

        GameObject go = NIFLoader.loadNIF("P_F_gloamwood_lamp_post_02.nif");
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
        NIFTexturePool.inst.process();
	}
}

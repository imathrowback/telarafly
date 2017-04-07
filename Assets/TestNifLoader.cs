using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestNifLoader : MonoBehaviour {

	// Use this for initialization
	void Start () {
        NIFLoader loader = new NIFLoader();
        GameObject go = loader.loadNIFFromFile(@"D:\rift_stuff\nif\A_C_keep_stillmoor_south_entry_01.nif");
        go.transform.parent = this.transform;

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}

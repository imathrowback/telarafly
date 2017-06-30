using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Reflection;

public class NIFTester : MonoBehaviour {

   

    // Use this for initialization
    void Start () {

        Material transmat = Resources.Load("transmat", typeof(Material)) as Material;
        //Debug.Log(transmat.color);
        string file = @"D:\rift_stuff\nif\A_C_keep_stillmoor_south_entry_01.nif";
      
        GameObject obj = NIFLoader.loadNIFFromFile(file);
        obj.transform.parent = this.gameObject.transform;
    }

    
    
    
    

    // Update is called once per frame
    void Update () {
		
	}
}

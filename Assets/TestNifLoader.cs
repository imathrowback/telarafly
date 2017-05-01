using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestNifLoader : MonoBehaviour {

	// Use this for initialization
	void Start () {
        NIFLoader loader = new NIFLoader();
        GameObject go = loader.loadNIFFromFile(@"C:\Users\Spikeles\Documents\NetBeansProjects\TelaraDBExplorer\TelaraDBEditorCore\elf_male_cloth_helmet_119.nif");
        go.transform.parent = this.transform;

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}

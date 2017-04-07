using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLoadWater : MonoBehaviour {

	// Use this for initialization
	void Start () {
		GetComponent<MeshRenderer>().material =  new Material(Resources.Load("Environment/Water/Water4/Materials/Water4Advanced", typeof(Material)) as Material);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}

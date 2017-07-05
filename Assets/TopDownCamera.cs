using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopDownCamera : MonoBehaviour {
    public Camera followTarget;
    public int yHeight;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 cPos = followTarget.transform.position;
        this.GetComponent<Camera>().transform.position = new Vector3(cPos.x, yHeight, cPos.z);
	}
}

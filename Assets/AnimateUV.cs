using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateUV : MonoBehaviour {
    float scrollSpeed = 0.5f;
    Renderer r;
    // Use this for initialization
    void Start () {
        r = GetComponent<Renderer>();
	}
	
	// Update is called once per frame
	void Update () {
        var offset = Time.time * scrollSpeed;
        r.material.mainTextureOffset = new Vector2(0, offset % 1);

    }
}

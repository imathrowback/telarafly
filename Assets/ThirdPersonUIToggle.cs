using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThirdPersonUIToggle : MonoBehaviour {
    [SerializeField]
    Toggle toggle;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void toggleThirdPerson()
    {
        Assets.GameWorld.useColliders = toggle.isOn;
    }
}

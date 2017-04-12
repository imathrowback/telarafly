using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class LightIntensityUIHandler : MonoBehaviour {
    [SerializeField]
    Light sourceLight;
    Slider slider;
	// Use this for initialization
	void Start () {
        slider = GetComponent<Slider>();
        slider.value = sourceLight.intensity;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void sliderChanged()
    {
        sourceLight.intensity = slider.value;
    }
}

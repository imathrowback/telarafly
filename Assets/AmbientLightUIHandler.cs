using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AmbientLightUIHandler : MonoBehaviour {
        Slider slider;
        // Use this for initialization
        void Start()
        {
            slider = GetComponent<Slider>();
            slider.value = RenderSettings.ambientLight.r;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void sliderChanged()
        {
        RenderSettings.ambientLight = new Color(slider.value, slider.value, slider.value);
        }
    }
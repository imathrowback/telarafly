using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Management;

public class VRInitializer : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;

    [Serializable]
    /// <summary>
    /// UnityEvent callback for when a toggle is toggled.
    /// </summary>
    public class VREnabledEvent : UnityEvent
    { }

    public VREnabledEvent eventHandler;
    void Start()
    {
    }

    public void doVR()
    { 
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
        
        foreach (GameObject go in objectsToDisable)
            go.SetActive(false);

        foreach (GameObject go in objectsToEnable)
            go.SetActive(true);

        eventHandler.Invoke();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screenshot : MonoBehaviour {
    public Canvas ui;
	// Use this for initialization
	void Start () {
		
	}
	
	IEnumerator doScreenshotF()
    {
        yield return new WaitForEndOfFrame();
        DateTime date = DateTime.Now;
        string dateStr = date.ToString("yyyyMMddHHmmss");
        Application.CaptureScreenshot("Screenshot"+ dateStr + ".png");
        ui.gameObject.SetActive(true);
    }

    void Update () {
        if (Input.GetButtonDown("Screenshot"))
        {
            ui.gameObject.SetActive(false);
            StartCoroutine(doScreenshotF());
        }

    }

    
}

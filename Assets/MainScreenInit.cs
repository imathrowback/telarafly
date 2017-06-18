using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainScreenInit : MonoBehaviour {

	// Use this for initialization
	void Start () {
        string[] args = System.Environment.GetCommandLineArgs();
        
        string input = "";
        for (int i = 0; i < args.Length; i++)
        {
            Debug.Log("ARG " + i + ": " + args[i]);
            if (args[i] == "-screenshotName")
            {
                Assets.ScreenshotData.hash = Assets.RiftAssets.Util.hashFileName(args[i + 1]);
                Assets.ScreenshotData.valid = true;
            }
            if (args[i] == "-screenshotHash")
            {
                Assets.ScreenshotData.hash = args[i + 1];
                Assets.ScreenshotData.valid = true;
            }
        }

        if (Assets.ScreenshotData.valid )
        {
            SceneManager.LoadScene("screenshot");
        }
        else
        {
            SceneManager.LoadScene("test-decomp");
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}

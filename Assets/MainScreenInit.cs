using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            else if (args[i] == "-screenshotHash")
            {
                Assets.ScreenshotData.hash = args[i + 1];
                Assets.ScreenshotData.valid = true;
            }
            else if (args[i] == "-objexpdir")
            {
                ExportModelData.outputDirectory = args[i + 1];
                if (!Directory.Exists(ExportModelData.outputDirectory))
                    throw new Exception("Invalid output directory: " + ExportModelData.outputDirectory);
                ExportModelData.valid = true;
            }
            else if (args[i] == "-objexpext")
            {
                ExportModelData.expectedTextureExtension = args[i + 1];
            }
            else if (args[i] == "-objexplangids")
            {
                string file = args[i + 1];
                if (File.Exists(file))
                {
                    foreach(string s in File.ReadAllLines(file))
                    {
                        ExportModelData.langIDs.Add(int.Parse(s));
                    }
                }
            }
        }
        

        if (ExportModelData.valid)
        {
            SceneManager.LoadScene("obj-export-all");
        }
        else if (Assets.ScreenshotData.valid )
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

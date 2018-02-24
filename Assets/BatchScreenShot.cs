using Assets.RiftAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BatchScreenShot : MonoBehaviour {

    // telarafly -batchmode -screen-height=1024 -screen-width=768 -screenshotName elf_male_cape_tempest.nif
    public Camera cam;
    public RenderTexture texture;
    /*
    void OnDrawGizmos()
    {
        Object[] rList = go.GetComponentsInChildren(typeof(Renderer));
        foreach (Renderer r in rList)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(r.bounds.center, r.bounds.extents.magnitude);
        }
    }
    */

    Bounds CalculateBounds(GameObject go)
    {
        Bounds b = new Bounds();
        bool bSet = false;
        Renderer[] rList = go.GetComponentsInChildren< Renderer>();
        foreach (Renderer r in rList)
        {
            if (!bSet)
            {
                b = r.bounds;
                bSet = true;
            }
            else
                b.Encapsulate(r.bounds);
        }
        return b;
    }
    void FocusCameraOnGameObject(Camera c, GameObject go, Vector3 dir)
    {
        Bounds b = CalculateBounds(go);
        Vector3 max = b.size;
        float radius = Mathf.Max(max.x, Mathf.Max(max.y, max.z));
        float dist = radius / (Mathf.Sin(c.fieldOfView * Mathf.Deg2Rad / 2f));
        //Debug.Log("Radius = " + radius + " dist = " + dist);
//        Vector3 pos = Random.onUnitSphere * dist + b.center;
        Vector3 pos = (dir * dist)+  b.center;
        Debug.DrawLine(pos, b.center);
        c.transform.position = pos;
        c.transform.LookAt(b.center);
    }

    // Use this for initialization
    void Start () {
        Debug.Log("start");
        // -batchMode 
        if (Assets.ScreenshotData.hash == null || Assets.ScreenshotData.hash.Length == 0)
            Assets.ScreenshotData.hash = Util.hashFileName("elf_male_cape_tempest.nif");
        //string str = "elf_male_cape_tempest.nif";
        //str = "elf_male_cloth_helmet_119.nif";
        Debug.Log("Trying to loading NIF with hash: " + Assets.ScreenshotData.hash);
       go = NIFLoader.loadNIF(Assets.ScreenshotData.hash);
        Debug.Log("Done NIF load");

        go.transform.position = Vector3.zero;
        //go.transform.parent = this.transform;
        Debug.Log("done load");
    }
    GameObject go;

    Vector3[] vecs =
    {
        Vector3.forward, Vector3.left, Vector3.right, Vector3.back, Vector3.up, Vector3.down
    };
    string[] names =
    {
        "forward", "left", "right", "back", "top", "bottom"
    };

    int state = 0;
    bool takeImage = false;
    // Update is called once per frame
    void Update() {

        try
        {
            Debug.Log("Focus image");
            FocusCameraOnGameObject(cam, go, vecs[state]);
            takeImage = true;
            cam.Render();
        }
        catch(Exception ex)
        {
            Debug.LogError(ex);
            Application.Quit();
        }



        //Application.CaptureScreenshot();



     
    }
     public void OnPostRender()
     {
        Debug.Log("post render: state = " + state + ": takeImage:" + takeImage);
        if (state < vecs.Length && takeImage)
        {
            takeImage = false;
            Texture2D tex = new Texture2D(4096, 4096, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 4096, 4096), 0, 0);

            //RenderTexture tex = cam.targetTexture;
            byte[] data = tex.EncodeToPNG();
            string patch = @"screenshot-" + names[state] + ".png";
            File.WriteAllBytes(patch, data);

            state++;
        }
        if (state >= vecs.Length)
        {
            Debug.Log("quit");
            // save any game data here
#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#else
         File.WriteAllText("done.txt", "done " + Assets.ScreenshotData.hash);
         Application.Quit();
#endif
        }

    }
    
}

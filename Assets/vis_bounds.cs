using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class vis_bounds : MonoBehaviour {
    Camera cam;
    public GameObject root;

    int frames = 100;
    int frame = 0;
    // Use this for initialization
    void Start () {
        cam = this.GetComponent<Camera>();
    }

    
    void Update() {
        if (frame++ > frames)
        {
            frame = 0;
            int cCount = root.transform.childCount;
            for (int i = 0; i < cCount; i++)
            {
                Transform t = root.transform.GetChild(i);
                showHide(t.gameObject, cam);
            }
        }
    }

    void showHide(GameObject obj, Camera cam)
    { 

        MeshRenderer[] mrs = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in mrs)
        {
            mr.enabled = false;
            Bounds bounds = mr.bounds;

            //Vector3 heading = bounds.center - cam.transform.position;

            //if (Vector3.Dot(cam.transform.forward, heading) > 0)
            {

                Vector3 min = bounds.min;
                Vector3 max = bounds.max;

                Vector3 sMin = cam.WorldToScreenPoint(min);
                Vector3 sMax = cam.WorldToScreenPoint(max);

                float pixels = Vector3.Distance(sMin, sMax);
                if (mr.gameObject.name.Equals("N_F_iron_pine_rock_bridge_0Shape2:0"))
                {
                    //Debug.Log(pixels);
                    
                    //Debug.DrawLine(min, max);
                }
                if (pixels > 40)
                    mr.enabled = true;
            }
        }
    }
}

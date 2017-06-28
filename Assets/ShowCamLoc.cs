using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowCamLoc : MonoBehaviour
{
    GameObject mcamera;
    //camera_movement camMov;
    public telera_spawner spawner;
    Text text;
    RectTransform rt;
    RectTransform background;
    private GameObject meshRoot;

    // Use this for initialization
    void Start () {
        mcamera = GameObject.Find("Main Camera");
        meshRoot = GameObject.Find("NIFRotationRoot");
        //camMov = mcamera.GetComponent<camera_movement>();
        //spawner = mcamera.GetComponent<telera_spawner>();
        rt = GetComponent<RectTransform>();
        text = GetComponent<Text>();
        background = GetComponentInChildren<RectTransform>();
    }

    public void OnClick()
    {
        //Debug.Log(getCamPos());
        TextEditor te = new TextEditor();
        te.text = getCamPos();
        te.SelectAll();
        te.Copy();
    }

    // Update is called once per frame
    void Update() {
        string x = "";

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit)) 
        {
            x += "Object: " + hit.collider.name;
        }



        
        x +="\nCamera position: " + getCamPos() ;

        if (spawner.ObjJobLoadQueueSize() > 0 )
        {
            //pos.y += 30;
            string paused = "";
            //if (camMov.isRotating)
            //    paused = " (paused) ";
            
            x += "\nMeshes loading: " + spawner.ObjJobLoadQueueSize() + "" + paused ;
        }
        //rt.position = pos;
        text.text = x;
      //  background.wi
        //background.bott
        //text.
	}

    private string getCamPos()
    {
        return meshRoot.transform.InverseTransformPoint(mcamera.transform.position).ToString();
    }
}

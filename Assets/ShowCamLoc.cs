using System.Collections;

using System.Collections.Generic;
using System.Text;
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
    Vector3 lastV = Vector3.zero;
    string lastC = "";
    // Update is called once per frame
    void Update() {
        StringBuilder x = new StringBuilder(30);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit)) 
        {
            x.Append("Object: " + hit.collider.name);
        }




        x.Append("\nCamera position: ");
        x.Append(getCamPos());

        if (spawner.ObjJobLoadQueueSize() > 0 )
        {
            //pos.y += 30;
            string paused = "";
            //if (camMov.isRotating)
            //    paused = " (paused) ";

            x.Append("\nMeshes loading: ");
            x.Append(spawner.ObjJobLoadQueueSize());
            x.Append("");
            x.Append(paused);
        }
        //rt.position = pos;
        text.text = x.ToString();
      //  background.wi
        //background.bott
        //text.
	}

    private string getCamPos()
    {
        Vector3 cPos = mcamera.transform.position;
        if (cPos.Equals(lastV))
            return lastC;
        Vector3 v = meshRoot.transform.InverseTransformPoint(cPos);
        this.lastV = v;
        this.lastC = v.ToString();
        return lastC;
    }
}

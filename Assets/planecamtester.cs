using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class planecamtester : MonoBehaviour {
    List<GameObject> objs;
	// Use this for initialization
	void Start () {
        objs = new List<GameObject>();
        for (int x= -100; x < 100; x+= 5)
        {
            for (int y = -100; y < 100; y += 5)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.position = new Vector3(x, 0, y);
                objs.Add(go);
            }

        }
    }
	
	// Update is called once per frame
	void Update () {
        Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes(this.GetComponent<Camera>());
        foreach(GameObject go in objs)
        {
            Vector3 pos = go.transform.position;
            go.SetActive(TestPlanesAABB(camPlanes, pos));
        }
    }

    private bool TestPlanesAABB(Plane[] camPlanes, Vector3 point)
    {
        foreach (Plane p in camPlanes)
        {
            if (!p.GetSide(point))
                return false;
        }
        return true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class emissive_test : MonoBehaviour {
    MeshRenderer mr;
    MeshRenderer mr2;
    // Use this for initialization
    void Start () {
        mr = GetComponent<MeshRenderer>();

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.isStatic = true;
        Material mat = new Material(Shader.Find("Standard"));
        mr2 = go.GetComponent<MeshRenderer>();
        mr2.material = mat;
    }
	
	// Update is called once per frame
	void Update () {
        float intensity = 6.1f;
        Color final = Color.green * Mathf.LinearToGammaSpace(intensity);

        mr.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mr.material.SetColor("_EmissionColor", final);
        DynamicGI.SetEmissive(mr, final);

        mr2.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mr2.material.SetColor("_EmissionColor", final);
        DynamicGI.SetEmissive(mr2, final);
        //DynamicGI.UpdateMaterials(mr);
        //DynamicGI.UpdateEnvironment();
    }
}

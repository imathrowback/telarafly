using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Assets.Database;
using Assets;
using System;

public class TelaraMaker   {
    [MenuItem("Assets/Create World")]
    static void Create()
    {
        GameObject meshRoot = GameObject.Find("NIFRotationRoot");
        cleanMeshRoot(meshRoot.transform);
        GameObject prefab = Resources.Load<GameObject>("telara_obj_prefab");

        string world = "pd_realm_of_the_fae";
        int maxX = 0;
        int maxY = 0;
        Assets.WorldStuff.CDRParse.getMinMax(world + "_map.cdr", ref maxX, ref maxY);
        for (int tx = 0; tx < maxX; tx += 256)
        {
            for (int ty = 0; ty < maxX; ty += 256)
            {
                Assets.WorldStuff.CDRParse.doWorldTile(AssetDatabaseInst.DB, DBInst.inst, world, tx , ty , (p) =>
                {
                    GameObject go = process(p, meshRoot, prefab);
                    
                    telara_obj tobj = go.GetComponent<telara_obj>();
                    
                    if (tobj != null)
                    {
                        NifLoadJob job = new NifLoadJob(p.nifFile);
                        job.parent = tobj;
                        Vector3 pos = go.transform.position;
                        job.parentPos = pos;

                        job.Start();
                        while (!job.Update())
                        {
                            go.isStatic = true;
                        }
                    }
                });
            }
        }
    }

    private static void cleanMeshRoot(Transform transform)
    {
        foreach (Transform child in transform)
        {
            destroy(child);
        }
    }

    private static void destroy(Transform transform)
    {
        foreach (Transform child in transform)
        {
            destroy(child);
        }
        GameObject.DestroyImmediate(transform.gameObject);
    }

    public static GameObject process(ObjectPosition op, GameObject meshRoot,GameObject telaraObjectPrefab)
    {

        if (op is LightPosition)
        {
            GameObject lgo = new GameObject();

            LightPosition lp = (LightPosition)op;
            lgo.transform.SetParent(meshRoot.transform);
            lgo.transform.localScale = new Vector3(op.scale, op.scale, op.scale);
            lgo.transform.localPosition = op.min;
            lgo.transform.localRotation = op.qut;

            Light light = lgo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(lp.r, lp.g, lp.b);
            light.intensity = lp.range;
            light.shadows = LightShadows.Hard;
            return lgo;
        }

    GameObject go = GameObject.Instantiate(telaraObjectPrefab, meshRoot.transform);


        string name = op.nifFile;
        Assets.RiftAssets.AssetDatabase.RequestCategory category = Assets.RiftAssets.AssetDatabase.RequestCategory.NONE;
        if (name.Contains("_terrain_") || name.Contains("ocean_chunk"))
        {
            if (name.Contains("_terrain_"))
                category = Assets.RiftAssets.AssetDatabase.RequestCategory.GEOMETRY;
        }

        telara_obj tobj = go.GetComponent<telara_obj>();
        tobj.setProps(category);

        //go.transform.SetParent(meshRoot.transform);

        tobj.setFile(name);
        go.name = name;
        go.transform.localScale = new Vector3(op.scale, op.scale, op.scale);
        go.transform.localPosition = op.min;
        go.transform.localRotation = op.qut;
        return go;

    }


}

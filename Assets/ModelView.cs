using Assets.NIF;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Ionic.Zlib;
using System;
using System.Reflection;

public class ModelView : MonoBehaviour
{
    public string niffilename = "hero_vigil_messenger.nif";
    //    string kfmfilename = "hero_vigil_messenger.kfm";
    public string kfbfilename = "hero_vigil_messenger_dual.kfb";
    public int animToUse = 0;
    NIFFile nifanimation;
    NIFLoader loader;
    // Use this for initialization
    Dictionary<String, GameObject> boneMap = new Dictionary<string, GameObject>();
    void Start()
    {
        loader = new NIFLoader();
        loader.loadManifestAndDB();

        GameObject root = GameObject.Find("ROOT");
        // kfb = hero_vigil_messenger_dual.kfb
        // kfm = hero_vigil_messenger.kfm
        // nif = hero_vigil_messenger.nif


        GameObject nifmodel = loader.loadNIF(niffilename);
        nifmodel.transform.parent = root.transform;

        //TODO: read KFM file
        //KFMFile kfm = new KFMFile(new MemoryStream(loader.db.extractUsingFilename(kfmfilename)));

        NIFFile kfb = new NIFFile(new MemoryStream(loader.db.extractUsingFilename(kfbfilename)));

        /** Choose the right animation to load from the KFB file. Ideally we should use the KFM to know what index to use */
        for (int i = 0; i < kfb.numObjects; i += 4)
        {
            NiIntegerExtraData indexData = (NiIntegerExtraData)kfb.getObject(i);
            NiIntegerExtraData sizeData = (NiIntegerExtraData)kfb.getObject(i + 1);
            NiBinaryExtraData binData = (NiBinaryExtraData)kfb.getObject(i + 2);
            NiBinaryExtraData binData2 = (NiBinaryExtraData)kfb.getObject(i + 3);

            int animIdx = indexData.intExtraData;
            if (animIdx == animToUse)
            {
                nifanimation = new NIFFile(new MemoryStream(binData.decompressed));
                break;
            }

        }
    }

    private void doFrame(float t)
    {
        /** For each sequence, evaluate it with the current time and apply the result to the related bone */
        foreach (NiSequenceData data in nifanimation.nifSequences)
        {
            for (int i = 0; i < data.seqEvalIDList.Count; i++)
            {
                int evalID = (int)data.seqEvalIDList[i];
                NiBSplineCompTransformEvaluator evalObj = (NiBSplineCompTransformEvaluator)nifanimation.getObject(evalID);
                string boneName = nifanimation.getStringFromTable(evalObj.m_kAVObjectName);
                GameObject go;
                // cache game objects for bones
                if (boneMap.ContainsKey(boneName))
                    go = boneMap[boneName];
                else
                {
                    go = boneMap[boneName] = GameObject.Find(boneName);
                }
                if (go == null)
                {
                    //Debug.Log("unable to get gameobject for bone " + boneName);
                    continue;
                }
                int splineDataIndex = evalObj.splineDataIndex;
                int basisDataIndex = evalObj.basisDataIndex;

                if (splineDataIndex != -1 && basisDataIndex != -1)
                {
                    NiBSplineData splineObj = (NiBSplineData)nifanimation.getObject(splineDataIndex);
                    NiBSplineBasisData basisObj = (NiBSplineBasisData)nifanimation.getObject(basisDataIndex);
                    if (evalObj.m_kRotCPHandle != 65535)
                    {
                        float[] afValues = new float[4];
                        // get the rotation values for the given time 't'
                        splineObj.getCompactValueDegree3(t, afValues, 4, basisObj, evalObj.m_kRotCPHandle,
                            evalObj.m_afCompScalars[(int)NiBSplineCompTransformEvaluator.COMP_.ROTATION_OFFSET],
                            evalObj.m_afCompScalars[(int)NiBSplineCompTransformEvaluator.COMP_.ROTATION_RANGE]);
                        // apply the rotation to the bone
                        go.transform.localRotation = new Quaternion(afValues[1], afValues[2], afValues[3], afValues[0]);
                    }
                    if (evalObj.m_kTransCPHandle != 65535)
                    {
                        // get the position values for the given time 't'
                        float[] afValues = new float[3];
                        float offset = evalObj.m_afCompScalars[(int)NiBSplineCompTransformEvaluator.COMP_.POSITION_OFFSET];
                        float range = evalObj.m_afCompScalars[(int)NiBSplineCompTransformEvaluator.COMP_.POSITION_RANGE];
                        splineObj.getCompactValueDegree3(t, afValues, 3, basisObj, evalObj.m_kTransCPHandle,
                                offset,
                                range);
                        go.transform.localPosition = new Vector3(afValues[0], afValues[1], afValues[2]);

                    }
                }
            }
        }

    }

    private void log(Vector3 localPosition)
    {
        String s = string.Format("{0:0.000000},{1:0.000000},{2:0.000000}", localPosition.x, localPosition.y, localPosition.z);
        Debug.Log(s);
    }
    private string format(Quaternion localPosition)
    {
        return string.Format("{0:0.000000},{1:0.000000},{2:0.000000},{3:0.000000}", localPosition.x, localPosition.y, localPosition.z, localPosition.w);
    }



    static object getField(object obj, string fieldName)
    {
        Type type = obj.GetType();
        FieldInfo field = type.GetField(fieldName);
        if (field != null)
            return field.GetValue(obj);
        return null;
    }

    // Update is called once per frame
    float tt = 0;
    void FixedUpdate()
    {
        tt += 0.02f;
        if (tt > 1)
            tt = 0;
        doFrame(tt);

    }


}

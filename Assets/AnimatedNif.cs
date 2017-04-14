using Assets.NIF;
using Assets.RiftAssets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets
{
    class AnimatedNif
    {
        Dictionary<String, GameObject> boneMap = new Dictionary<string, GameObject>();

        public string nif;
        string kfm;
        string kfb;
        AssetDatabase adb;

        KFMFile kfmfile;
        NIFFile kfbfile;
        NIFFile nifanimation;
        int activeAnimation = -1;
        List<KFAnimation> anims;
        public List<KFAnimation> getAnimations()
        {
            //Debug.Log("get animations");
            if (anims != null)
                return anims;

            if (kfmfile == null)
                kfmfile = new KFMFile(new MemoryStream(adb.extractUsingFilename(kfm)));

            anims = new List<KFAnimation>();
            System.Diagnostics.Stopwatch sp = System.Diagnostics.Stopwatch.StartNew();
            sp.Start();
            foreach (KFAnimation anim in kfmfile.kfanimations)
            {
                int id = anim.id;
                //Debug.Log("[" + sp.ElapsedMilliseconds + "] check id[" + id + "]");
                if (getData(id) != null)
                {
                    anims.Add(anim);
                }
                // Debug.Log("[" + sp.ElapsedMilliseconds + "] done check id[" + id + "]");
            }

            return anims;
        }

        public AnimatedNif(AssetDatabase adb, string nif, string kfm, string kfb)
        {
            this.adb = adb;
            this.nif = nif;
            this.kfm = kfm;
            this.kfb = kfb;
        }

        private byte[] getData(int animToUse)
        {
            if (kfbfile == null)
                kfbfile = new NIFFile(new MemoryStream(adb.extractUsingFilename(this.kfb)));

            /** Choose the right animation to load from the KFB file. Ideally we should use the KFM to know what index to use */
            for (int i = 0; i < kfbfile.numObjects; i += 4)
            {
                NiIntegerExtraData indexData = (NiIntegerExtraData)kfbfile.getObject(i);
                NiIntegerExtraData sizeData = (NiIntegerExtraData)kfbfile.getObject(i + 1);
                NiBinaryExtraData binData = (NiBinaryExtraData)kfbfile.getObject(i + 2);
                NiBinaryExtraData binData2 = (NiBinaryExtraData)kfbfile.getObject(i + 3);

                int animIdx = indexData.intExtraData;
                if (animIdx == animToUse)
                    return binData.decompressed;
            }
            return null;
        }

        public NIFFile loadKFB(int animToUse)
        {
            byte[] data = getData(animToUse);
            if (data != null)
                return new NIFFile(new MemoryStream(data));
            Debug.Log("unable to load KFB for anim:" + animToUse);
            return null;
        }

        public void setActiveAnimation(string anim)
        {
            foreach (KFAnimation kfa in getAnimations())
                if (kfa.sequencename.Equals(anim))
                {
                    setActiveAnimation(kfa.id);
                    return;
                }
            Debug.Log("Unable to find animation " + anim);
        }

        public void setActiveAnimation(int anim)
        {
            //Debug.Log("set anim to " + anim);
            nifanimation = loadKFB(anim);
            this.activeAnimation = anim;
            boneMap.Clear();
        }

        public int getIdleAnimIndex()
        {
            foreach (KFAnimation kfa in getAnimations())
                if (kfa.sequencename.Contains("idle"))
                    return kfa.id;
            return 0;
        }

        public void doFrame(float t)
        {
            if (nifanimation == null || activeAnimation == -1)
            {
                setActiveAnimation(getIdleAnimIndex());
                if (nifanimation == null)
                {
                    return;
                }
            }

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

    }
}

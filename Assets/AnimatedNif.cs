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
    public class AnimatedNif : MonoBehaviour
    {
        Dictionary<String, GameObject> boneMap = new Dictionary<string, GameObject>();

        public string nif;
        string kfm;
        string kfb;
        AssetDatabase adb = AssetDatabaseInst.DB;

        KFMFile kfmfile;
        NIFFile kfbfile;
        NIFFile nifanimation;
        int activeAnimation = -1;
        List<KFAnimation> anims;
        GameObject skeletonRoot;

        public void setParams(AssetDatabase adb, string nif, string kfm, string kfb)
        {
            this.adb = adb;
            this.nif = nif;
            this.kfm = kfm;
            this.kfb = kfb;
            kfmfile = null;
            kfbfile = null;
            nifanimation = null;
            activeAnimation = -1;
            skeletonRoot = null;
            anims = null;
            boneMap.Clear();
        }

        public void clearBoneMap()
        {
            boneMap.Clear();
        }

        public void setSkeletonRoot(GameObject root)
        {
            if (root == null)
                throw new Exception("attempt to set null skeleton");
            this.skeletonRoot = root;
        }
        public List<KFAnimation> getAnimations()
        {
            if (anims != null)
                return anims;

            if (kfm == null)
                return new List<KFAnimation>();

            if (kfmfile == null)
                kfmfile = new KFMFile(new MemoryStream(adb.extractUsingFilename(kfm)));

            anims = new List<KFAnimation>();
            //System.Diagnostics.Stopwatch sp = System.Diagnostics.Stopwatch.StartNew();
            //sp.Start();
            foreach (KFAnimation anim in kfmfile.kfanimations)
            {

                int id = anim.id;
                //Debug.Log("[" + sp.ElapsedMilliseconds + "] check id[" + id + "]");
                byte[] data = getKFBData(id);
                if (data != null)
                    anims.Add(anim);
                //Debug.Log("Found anim [" + anim.id + "]:" + anim.sequenceFilename + ":" + anim.sequencename +": hasData:" + (data != null));
                // Debug.Log("[" + sp.ElapsedMilliseconds + "] done check id[" + id + "]");
            }

            return anims;
        }
        public AnimatedNif()
        {

        }
       
        public AnimatedNif(AssetDatabase adb, string nif, string kfm, string kfb)
        {
            setParams(adb, nif, kfm, kfb);
        }

        /** Load the KFB data for the selected animation, will return null if the animation does not exist in the KFB */
        private byte[] getKFBData(int animToUse)
        {
            if (kfb == null)
                return null;
            if (kfbfile == null)
            {
                if (this.adb.filenameExists(this.kfb))
                {
                    kfbfile = NIFLoader.getNIF(this.kfb);
                }
                else
                    return null;
                //Debug.Log("getting KFB: " + this.kfb);
            }
            /** Choose the right animation to load from the KFB file. Ideally we should use the KFM to know what index to use */
            for (int i = 0; i < kfbfile.numObjects; i += 4)
            {
                NiIntegerExtraData indexData = (NiIntegerExtraData)kfbfile.getObject(i);
                NiIntegerExtraData sizeData = (NiIntegerExtraData)kfbfile.getObject(i + 1);
                NiBinaryExtraData binData = (NiBinaryExtraData)kfbfile.getObject(i + 2);
                NiBinaryExtraData binData2 = (NiBinaryExtraData)kfbfile.getObject(i + 3);

                int animIdx = indexData.intExtraData;
                if (animIdx == animToUse)
                    return binData.getData();
            }
            //Debug.LogError("[" + this.kfb + "] can't find data for anim[" + animToUse + "]");
            return null;
        }

        private NIFFile loadKFB(int animToUse)
        {
            byte[] data = getKFBData(animToUse);
            if (data != null)
            {
                //Debug.Log("load KFB data[" + data.Length + "] for anim[" + animToUse + "], from " + this.kfb);
                //File.WriteAllBytes("kfb.data", data);
                return new NIFFile(new MemoryStream(data));
            }
            //Debug.Log("unable to load KFB for anim:" + animToUse);
            return null;
        }

        public void setActiveAnimation(string anim)
        {
            //Debug.Log("set anim to " + anim);
            foreach (KFAnimation kfa in getAnimations())
                if (kfa.sequencename.Equals(anim))
                {
                    setActiveAnimation(kfa.id);
                    return;
                }
            //Debug.Log("Unable to find animation " + anim);
            //foreach (KFAnimation kfa in getAnimations())
            //    Debug.Log("\t " + kfa.sequencename);
        }

        public void setActiveAnimation(int anim)
        {
            //Debug.Log("set anim to " + anim);
            nifanimation = loadKFB(anim);
            this.activeAnimation = anim;
            boneMap.Clear();
        }

        public KFAnimation getActiveAnimation()
        {
            foreach (KFAnimation kfa in getAnimations())
                if (kfa.id == this.activeAnimation)
                    return kfa;
            return null;
        }

        /** Attempt to get an idle animation index. If no idle animation can be found, return 0 */
        public int getIdleAnimIndex()
        {
            foreach (KFAnimation kfa in getAnimations())
            {
                if (kfa.sequencename.Contains("unarmed_idle") || kfa.sequenceFilename.Contains("unarmed_idle"))
                    return kfa.id;
            }
            return 0;
        }
        public void zeroFrame()
        {
            this.tt = 0;
        }

        public void doFrame(float t)
        {
            if (skeletonRoot == null)
            {
                Debug.LogError("no skeleton root");
                return;
            }
            if (nifanimation == null || activeAnimation == -1)
            {
                setActiveAnimation(getIdleAnimIndex());
                if (nifanimation == null)
                {
                    return;
                }
            }
            //Debug.Log("using anim :" + this.nifanimation.)

            /** For each sequence, evaluate it with the current time and apply the result to the related bone */
            foreach (NiSequenceData data in nifanimation.nifSequences)
            {
                for (int i = 0; i < data.seqEvalIDList.Count; i++)
                {
                    int evalID = (int)data.seqEvalIDList[i];
                    NIFObject evalObjA = nifanimation.getObject(evalID);
                    if (nifanimation.getObject(evalID) is NiBSplineCompTransformEvaluator)
                    {
                        NiBSplineCompTransformEvaluator evalObj = (NiBSplineCompTransformEvaluator)evalObjA;
                        string boneName = nifanimation.getStringFromTable(evalObj.m_kAVObjectName);
                        GameObject go;
                        // cache game objects for bones
                        boneMap.TryGetValue(boneName, out go);
                        if (go == null)
                        {
                            Transform bone = skeletonRoot.transform.FindDeepChild(boneName);
                            if (bone == null)
                            {
//                                Debug.LogWarning("unable to find bone in skeleton for " + boneName);
                                continue;
                            }
                            go = boneMap[boneName] = bone.gameObject;
                            //GameObject.Find(boneName);
                        }
                        if (go == null)
                        {
  //                          Debug.Log("unable to get gameobject for bone " + boneName);
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
                        //Debug.Log("drawLine", go);
                        Debug.DrawLine(go.transform.position, go.transform.parent.position);

                    }
                    else
                        Debug.LogWarning("Unknown animation type:" + evalObjA.GetType());
                }
            }

        }

        float tt = 0;
        public float animSpeed = 0.02f;
        void FixedUpdate()
        {
            tt += animSpeed;
            if (tt > 1)
                tt = 0;
            doFrame(tt);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.RiftAssets;
using Assets.Database;
using Assets;
using Assets.NIF;
using System.IO;

public class test_kfb_kfm : MonoBehaviour {

	// Use this for initialization
	void Start () {
        AssetDatabase db = AssetDatabaseInst.DB;

        KFMFile kfm = new KFMFile(new FileStream(@"C:\Users\Spikeles\Documents\NetBeansProjects\TelaraDBExplorer\TelaraDBEditorCore\human_female_medium.kfm", FileMode.Open, FileAccess.Read, FileShare.Read));
        NIFFile kfbfile = new NIFFile(new FileStream(@"C:\Users\Spikeles\Documents\NetBeansProjects\TelaraDBExplorer\TelaraDBEditorCore\human_female_2h_shared.kfb", FileMode.Open, FileAccess.Read, FileShare.Read));

        // 230, string -> index 2

        List<KFAnimation> anims = kfm.kfanimations;
        int maxAnimID = 0;
        foreach (KFAnimation anim in anims)
        {
            maxAnimID = Mathf.Max(anim.id, maxAnimID);
            //Debug.Log(anim.id + ":" + anim.sequenceFilename + ":" + anim.sequencename);
        }
        Debug.Log("maxAnimID:" + maxAnimID);

        for (int i = 0; i < kfbfile.numObjects; i += 4)
        {

            NiIntegerExtraData indexData = (NiIntegerExtraData)kfbfile.getObject(i);
            NiIntegerExtraData sizeData = (NiIntegerExtraData)kfbfile.getObject(i + 1);
            NiBinaryExtraData binData = (NiBinaryExtraData)kfbfile.getObject(i + 2);
            NiBinaryExtraData binData2 = (NiBinaryExtraData)kfbfile.getObject(i + 3);
            KFAnimation anim = anims.DefaultIfEmpty(null).FirstOrDefault(a => a.id == indexData.intExtraData);
            if (anim != null)
                Debug.Log("kfb[" + indexData.intExtraData + "] match => [" + anim.id + "]" + anim.sequenceFilename);
            else
                Debug.Log("kfb[" + indexData.intExtraData + "] nomatch");
        }
        Debug.Log("kfb objs:" + kfbfile.numObjects/4);
        Debug.Log("anims:" + anims.Count);
        //File.WriteAllBytes("human_female.kfb" + i + "_0", binData.getData());
        //File.WriteAllBytes("human_female.kfb" + i + "_1", binData2.getData());

    }

    /*
    private byte[] getKFBData(NIFFile kfb)
    {
        for (int i = 0; i < kfbfile.numObjects; i += 4)
        {
            NiIntegerExtraData indexData = (NiIntegerExtraData)kfbfile.getObject(i);
            NiIntegerExtraData sizeData = (NiIntegerExtraData)kfbfile.getObject(i + 1);
            NiBinaryExtraData binData = (NiBinaryExtraData)kfbfile.getObject(i + 2);
            NiBinaryExtraData binData2 = (NiBinaryExtraData)kfbfile.getObject(i + 3);

            int animIdx = indexData.intExtraData;
            if (animIdx == animToUse)
                return binData.getDecompressed();
        }
        return null;
*/
    // Update is called once per frame
    void Update () {
		
	}
}

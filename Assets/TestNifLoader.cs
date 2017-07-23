using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets.Export;
using Assets.RiftAssets;
using Assets;
using System.IO;
using Assets.Wardrobe;
using Assets.Database;
using Assets.NIF;
using Assets.DatParser;

public class TestNifLoader : MonoBehaviour {
    GameObject mount;
    // Use this for initialization
    void Start () {
        GameObject go = NIFLoader.loadNIF("N_W_freemarch_kings_retreat_01.nif");
    }
	
	// Update is called once per frame
	void Update () {
        if (true)
            return; 
		if (mount == null)
        {

            KFMFile fa = new KFMFile(new FileStream(@"L:\Projects\riftools\RiftTools\build\jar\output\bahmi_female.kfmA", FileMode.Open));
            KFMFile fb = new KFMFile(new FileStream(@"L:\Projects\riftools\RiftTools\build\jar\output\bahmi_female.kfmB", FileMode.Open));

            StreamWriter sw = new StreamWriter("kfma.txt");
            foreach (KFAnimation b in fa.kfanimations.OrderBy(x => x.sequencename))
                sw.WriteLine(b.id + ":" + b.sequencename + ":" + b.sequenceFilename);
            sw.Close();
            sw = new StreamWriter("kfmb.txt");

            foreach (KFAnimation b in fb.kfanimations.OrderBy(x => x.sequencename))
                sw.WriteLine(b.id + ":" + b.sequencename + ":" + b.sequenceFilename);
            sw.Close();

            if (true)
                return;

            DBInst.inst.GetHashCode();

            mount = AnimatedModelLoader.loadNIF(1066487579);
//            mount = AnimatedModelLoader.loadNIF(1823429099);
            AnimatedNif animNif = mount.GetComponent<AnimatedNif>();
            animNif.animSpeed = 0.005f;
            animNif.setSkeletonRoot(mount);
            animNif.setActiveAnimation("mount_haunted_carriage_idle");
            mount.transform.localRotation = Quaternion.identity;
            mount.transform.localPosition = new Vector3(0, -5.91f, 7.66f);

            GameObject character = new GameObject();

            Paperdoll mainPaperdoll = character.AddComponent<Paperdoll>();

            mainPaperdoll.animOverride = "mount_haunted_carriage_idle";
            //mainPaperdoll.kfbOverride = "bahmi_male_mount.kfb";
            mainPaperdoll.setKFBPostFix("mount");
            mainPaperdoll.setGender("male");
            mainPaperdoll.setRace("bahmi");
            //mainPaperdoll.GetComponent<AnimatedNif>().animSpeed = 0.001f;
            mainPaperdoll.animSpeed = 0.005f;
            character.transform.parent = mount.transform;
            character.transform.localPosition = new Vector3(0, 0, 0);
            character.transform.localRotation = Quaternion.identity;
            mainPaperdoll.transform.localRotation = Quaternion.identity;

            if (false)
            {
                mainPaperdoll.FixedUpdate();
                List<KFAnimation> anims = mainPaperdoll.getAnimations();
                Debug.Log("===> anims:" + anims.Count + " ==>" + mainPaperdoll.getState());
                foreach (KFAnimation kf in anims)
                {
                    string s = (kf.sequenceFilename + ":" + kf.sequencename);
                    if (s.Contains("carriage"))
                        Debug.Log(s);
                }
            }

            //mainPaperdoll.updateRaceGender();

            //mainPaperdoll.setAppearenceSet(623293935);
            mainPaperdoll.setAppearenceSet(1044454339);
        }
    }
}

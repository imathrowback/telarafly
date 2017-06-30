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

public class TestNifLoader : MonoBehaviour {
    GameObject mount;
    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
		if (mount == null)
        {
            DBInst.inst.GetHashCode();

            mount = AnimatedModelLoader.loadNIFFromFile(@"L:\Projects\riftools\RiftTools\build\jar\output\9b5ef705-c4256efcbd38f8a8-B.file", "mount_disc.kfm", "mount_disc_mount.kfb");
            AnimatedNif animNif = mount.GetComponent<AnimatedNif>();
            animNif.animSpeed = 0.001f;
            animNif.setSkeletonRoot(mount);
            animNif.setActiveAnimation("mount_disc_idle");
            mount.transform.localRotation = Quaternion.identity;
            mount.transform.localPosition = new Vector3(0, -5.91f, 7.66f);

            GameObject character = new GameObject();

            Paperdoll mainPaperdoll = character.AddComponent<Paperdoll>();
            mainPaperdoll.animOverride = "mount_disc_dance_2";
            mainPaperdoll.kfbOverride = "human_female_mount.kfb";
            mainPaperdoll.setGender("female");
            mainPaperdoll.setRace("human");
            //mainPaperdoll.GetComponent<AnimatedNif>().animSpeed = 0.001f;
            mainPaperdoll.animSpeed = 0.001f;
            character.transform.parent = mount.transform;
            character.transform.localPosition = new Vector3(0, 0, 0);
            character.transform.localRotation = Quaternion.identity;
            mainPaperdoll.transform.localRotation = Quaternion.identity;

            mainPaperdoll.updateRaceGender();
            mainPaperdoll.loadAppearenceSet(623293935);
        }
	}
}

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
    GameObject test;
    GameObject tail;
    AnimatedNif animNif;
    public long rweaponKey = 1072509202;
    // Use this for initialization
    void Start () {

        NIFLoader.loadNIF("pd_tower_meadow_terrain_1024_256.nif");

        //this.test = NIFLoader.loadNIF("elf_giant_unseelie_king.nif");
        //GameObject character = new GameObject();

        /*
        Paperdoll mainPaperdoll = character.AddComponent<Paperdoll>();


        mainPaperdoll.setGender("female");
        mainPaperdoll.setRace("human");
        //mainPaperdoll.GetComponent<AnimatedNif>().animSpeed = 0.001f;
        mainPaperdoll.animSpeed = 0.005f;
        character.transform.localPosition = new Vector3(0, 0, 0);
        character.transform.localRotation = Quaternion.identity;
        mainPaperdoll.transform.localRotation = Quaternion.identity;
        //mainPaperdoll.setAppearenceSet(1044454339);
        mainPaperdoll.setGearSlotKey(GearSlot.TORSO, 1169930480);
        //mainPaperdoll.setGearSlotKey(GearSlot.CAPE, 2131680782);
        //mainPaperdoll.setGearSlotKey(GearSlot.RANGED, 1072509202);
        //mainPaperdoll.setKFBPostFix("ranged_bow");
        //mainPaperdoll.clearGearSlot(GearSlot.HEAD);
        */

        //mainPaperdoll.forceNifForSlot(GearSlot.CAPE, "human_female_tail_001.nif");
        //tail = new GameObject();
        //NIFLoader.loadNIF("crucia.nif");


    }

    // Update is called once per frame
    void Update () {
        NIFTexturePool.inst.process();
        
    }
}

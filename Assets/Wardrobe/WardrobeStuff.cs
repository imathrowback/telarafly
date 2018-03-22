using Assets.Database;
using Assets.DatParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Wardrobe
{
    // Dataset 230?
    public enum GearSlot
    {
       
        MAIN_HAND = 1,
        OFF_HAND = 2,
        TWO_HAND = 3,
        RANGED = 4,
        
        SHOULDER = 6,
        TORSO = 7,
        LEGS = 8,
        HANDS = 9,
        FEET = 11,
        HEAD = 20,
        HEAD2 = 5,
        HAIR=23,
        FACIAL_HAIR=24,
        CAPE = 29,

    }

    // 2 - cloth
    // 3 - chain
    // 4 - plate
    // 5 - leather
    // 10 - vanity/costume
    public enum GearType
    {
        UNK = 0,
        CLOTH = 2,
        CHAIN = 3,
        PLATE = 4,
        LEATHER = 5,
        VANITY = 10
    }
    static public class WardrobeStuff
    {
        public static bool isWeapon(GearSlot slot)
        {
            return slot == GearSlot.MAIN_HAND || slot == GearSlot.OFF_HAND || slot == GearSlot.RANGED || slot == GearSlot.TWO_HAND;
        }
        public static Dictionary<string, int> raceMap = new Dictionary<string, int>();
        public static Dictionary<string, int> genderMap = new Dictionary<string, int>();
        public static Dictionary<GearSlot, string> weaponBindMap = new Dictionary<GearSlot, string>();

        static WardrobeStuff()
        {
            // initialize the race map       
            raceMap["human"] = 1;
            raceMap["elf"] = 2;
            raceMap["dwarf"] = 3;
            raceMap["bahmi"] = 2005;
            // whilst these are seperate races, they re-use existing models
            //raceMap["eth"] = 2007;
            //raceMap["highelf"] = 2008;
            genderMap["male"] = 0;
            genderMap["female"] = 2;

            //weaponBindMap[GearSlot.MAIN_HAND] = 
        }
        public static GearSlot getSlot(int i)
        {
            if (!Enum.IsDefined(typeof(GearSlot), i))
            {
                Debug.Log("Undefined gear slot:" + i);
            }

            return (GearSlot)i;
        }

        public static GearType getGearType(int i)
        {
            return (GearType)i;
        }
    }

    public static class DBWardrobeExtensions
    {
        public static IEnumerable<ClothingItem> getClothing(this DB db)
        {
            ClothingItem[] wardrobeItems = db.getEntriesForID(7629).Select(e => new ClothingItem(db,7629, e.key)).ToArray();
            Debug.Log("found " + wardrobeItems.Count() + " wardrobe items");
            HashSet<long> usedKeys = new HashSet<long>(wardrobeItems.Select(c => c.nifKey));

            ClothingItem[] modelItems = db.getEntriesForID(7305).Where(e => !usedKeys.Contains(e.key)).Select(e => new ClothingItem(db, 7305,e.key)).Where(ci => ci.allowedSlots.Count > 0).ToArray();
            Debug.Log(" found " + modelItems.Count() + " model items that could be wardrobe items");

            return wardrobeItems.Concat(modelItems);

        }
    }

    // type = 7629
    public class ClothingItem
    {
        public long id { get; }
        public long key { get; }
        public int langKey { get; }
        public string name { get; }
        public long nifKey;
        public NIFReference nifRef { get; }
        public List<GearSlot> allowedSlots = new List<GearSlot>();
        public GearType type = GearType.UNK;

        
        override public string ToString()
        {
            return "[" + id + "][" + key + "]:" + name;
        }
        public ClothingItem(DB db,  long key) : this(db, 7629, key)
        {
            
        }
        public ClothingItem(DB db, long id, long key)
        {
            this.key = key;
            this.id = id;
            try
            {

                CObject gearDef = db.toObj(id, key);
                if (id == 7629)
                {
                    this.name = gearDef.getMember(0).convert() + "";
                    if (gearDef.hasMember(1))
                    {
                        this.langKey = gearDef.getMember(1).getIntMember(0);
                    }
                    if (gearDef.hasMember(5))
                    {
                        CObject allowedSlotsArray = gearDef.getMember(5);
                        foreach (CObject o in allowedSlotsArray.members)
                        {
                            int slot = int.Parse(o.convert() + "");
                            allowedSlots.Add(WardrobeStuff.getSlot(slot));
                        }
                    }

                    if (gearDef.hasMember(6))
                        type = WardrobeStuff.getGearType(gearDef.getIntMember(6));

                    nifKey = gearDef.getIntMember(2);
                    nifRef = new NIFReference(db, nifKey);
                }
                else if (id == 7305)
                {
                    nifRef = new NIFReference(db, key);
                    nifKey = key;
                    if (gearDef.hasMember(2))
                    {
                        string nifPath = gearDef.getMember(2).convert() + "";
                        this.name = System.IO.Path.GetFileName(nifPath);
                    }
                    if (gearDef.hasMember(15))
                    {
                        CObject allowedSlotsArray = gearDef.getMember(15);
                        foreach (CObject o in allowedSlotsArray.members)
                        {
                            int slot = int.Parse(o.convert() + "");
                            allowedSlots.Add(WardrobeStuff.getSlot(slot));
                        }
                    }

                }
                else throw new Exception("Bad id:" + id);
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to process [" + this + "] nif reference:" + ex);
            }
        }
    }
    public class NIFReference
    {
        string baseName;
        Dictionary<int, Dictionary<int, string>> nifs = new Dictionary<int, Dictionary<int, string>>();

        public string getNif(int race, int gender)
        {
            if (nifs.Count() > 0)
            {
                Dictionary<int, string> nifD = nifs[race];
                if (nifD.ContainsKey(gender))
                    return nifD[gender];
                {
                    Debug.LogWarning("No gender[" + gender + "] NIF for model " + baseName);
                    return nifD.First().Value;
                }
            }
            return baseName;
        }

        public NIFReference(DB db, long key)
        {
            CObject nifObj = db.toObj( 7305, key);

            baseName = nifObj.getMember(2).convert().ToString();
            if (nifObj.hasMember(5))
            {
                Dictionary<int, CObject> dict = nifObj.getMember(5).asDict();

                foreach (int race in dict.Keys)
                {
                    CObject nifRaceObj = dict[race];
                    nifs[race] = new Dictionary<int, string>();

                    string malenif = nifRaceObj.getMember(0).convert().ToString();
                    if (nifRaceObj.hasMember(2))
                    {
                        string femalenif = nifRaceObj.getMember(2).convert().ToString();
                        nifs[race][2] = femalenif;
                    }

                    nifs[race][0] = malenif;
                }
            }
        }
    }
}

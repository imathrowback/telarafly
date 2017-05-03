using Assets;
using Assets.DatParser;
using Assets.Database;
using Assets.NIF;
using Assets.RiftAssets;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Assets.Wardrobe;
//using deep
public class Wardrobe : MonoBehaviour
{
    DB db;
    
    
    public Text text;
   
    string progress;

    public Paperdoll paperDoll;
    
    public Dropdown appearanceDropdown;
    public Dropdown genderDropdown;
    public Dropdown raceDropdown;
   
    string raceString;
    string genderString;
  
    // Use this for initialization
    void Start()
    {

       

        raceString = "human";
        genderString = "male";

        genderDropdown.ClearOptions();
        genderDropdown.AddOptions(WardrobeStuff.genderMap.Keys.ToList());
        raceDropdown.ClearOptions();
        raceDropdown.AddOptions(WardrobeStuff.raceMap.Keys.ToList());
        appearanceDropdown.ClearOptions();
        updateRaceGender();

        DBInst.progress += (m) => progress = m;
        DBInst.loadedCallback += (d) => db = d;
    }

    public void updateRaceGender()
    {
        raceString = raceDropdown.options[raceDropdown.value].text;
        genderString = genderDropdown.options[genderDropdown.value].text;

        paperDoll.setGender(genderString);
        paperDoll.setRace(raceString);
        paperDoll.updateRaceGender();

        // reapply the costume
        changeAppearance();
    }

    bool first = false;
    // Update is called once per frame
    void Update()
    {
        if (text != null)
            text.text = progress;

        if (db != null && !first)
        {
            first = true;
            // finally everything is loaded and ready so lets load an appearence set
            try
            {
                List<DOption> options = new List<DOption>();
                foreach (entry e in db.getEntriesForID(7638))
                {
                    CObject _7637 = db.toObj(e.id, e.key);
                    string str = _7637.getMember(0).convert().ToString();
                    DOption option = new DOption();
                    option.text = str;
                    option.userObject = e;
                    options.Add(option);
                }

                options.Sort((a, b) => string.Compare(a.text, b.text));
                appearanceDropdown.AddOptions(options.Cast<Dropdown.OptionData>().ToList());
                    
            }
            catch (Exception ex)
            {
                Debug.Log("failed to load appearence set: " + ex);
            }
        }
    }

    public void changeAppearance()
    {
        if (appearanceDropdown.options.Count == 0)
            return;
        int v = appearanceDropdown.value;
        DOption option = (DOption)appearanceDropdown.options[v];
        entry entry =(entry) option.userObject;
        paperDoll.loadAppearenceSet(entry.key, WardrobeStuff.raceMap[raceString], WardrobeStuff.genderMap[genderString]);
    }

  

    class DOption : Dropdown.OptionData
    {
        public object userObject { get; set; }
    }
}

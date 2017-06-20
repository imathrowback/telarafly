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
    public Dropdown slotChangeDropdown;
    public Dropdown appearanceDropdown;
    public Dropdown genderDropdown;
    public Dropdown raceDropdown;
    ClothingItem[] clothingItems;
    string raceString;
    string genderString;
    Text pageText;
    
    List<ClothingItemRenderer> clothingPanels = new List<ClothingItemRenderer>();
    int previewIndex = 0;
  
    // Use this for initialization
    void Start()
    {
        GameObject previewPanel = GameObject.Find("PreviewPanel");
        pageText = GameObject.Find("PageText").GetComponent<Text>();
        clothingPanels.AddRange(previewPanel.GetComponentsInChildren<ClothingItemRenderer>());


        raceString = "human";
        genderString = "male";

        genderDropdown.ClearOptions();
        genderDropdown.AddOptions(WardrobeStuff.genderMap.Keys.ToList());
        raceDropdown.ClearOptions();
        raceDropdown.AddOptions(WardrobeStuff.raceMap.Keys.ToList());
        appearanceDropdown.ClearOptions();
        updateRaceGender();

        slotChangeDropdown.ClearOptions();
        List<DOption> slotOptions = new List<DOption>();
        foreach (GearSlot slot in Enum.GetValues(typeof(GearSlot)))
        {
            DOption option = new DOption();
            option.text = slot.ToString();
            option.userObject = slot;
            slotOptions.Add(option);
        }
        slotChangeDropdown.AddOptions(slotOptions.Cast<Dropdown.OptionData>().ToList());

        DBInst.progress += (m) => progress = m;
        DBInst.loadOrCallback((d) => db = d);
    }
    public void clickLeft()
    {
        previewIndex-= clothingPanels.Count();
        if (previewIndex < 0)
            previewIndex = 0;
        updatePreview();
    }
    public void clickRight()
    {
        previewIndex += clothingPanels.Count();
        if (previewIndex > clothingItems.Count()- clothingPanels.Count())
            previewIndex = clothingItems.Count() - clothingPanels.Count();
        updatePreview();
    }
    public void changeSlot()
    {
        DOption option = (DOption)slotChangeDropdown.options[slotChangeDropdown.value];
        GearSlot slot = (GearSlot)option.userObject;
        clothingItems = db.getClothing().Where(c => c.allowedSlots.Contains(slot)).ToArray();
        previewIndex = 0;
        updatePreview();
    }

    void updatePage()
    {
        pageText.text = "Items " + previewIndex + "-" + (previewIndex+clothingPanels.Count()) + " of " + clothingItems.Length;
    }
    public void mainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("test-decomp");

    }
    void updatePreview()
    {
        updatePage();
        for (int i = 0; i < clothingPanels.Count(); i++)
        {
            ClothingItem item = clothingItems[previewIndex + i];
            Debug.Log("set panel[" + i + "] to " + item);
            clothingPanels[i].setItem(item);
        }
    }

    public void updateRaceGender()
    {
        raceString = raceDropdown.options[raceDropdown.value].text;
        genderString = genderDropdown.options[genderDropdown.value].text;

        paperDoll.setGender(genderString);
        paperDoll.setRace(raceString);
        paperDoll.updateRaceGender();
        if (clothingPanels != null)
            foreach (ClothingItemRenderer r in clothingPanels)
            {
                if (r.previewPaperdoll != null)
                {
                    r.previewPaperdoll.updateRaceGender();
                    r.refresh();
                }
            }
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
                changeSlot();

                updatePreview();
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

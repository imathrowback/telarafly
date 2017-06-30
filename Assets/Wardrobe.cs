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
    WardrobePreviewPanelUpdater panelUpdater;
    public Paperdoll paperDoll;
    public Dropdown slotChangeDropdown;
    public Dropdown appearanceDropdown;
    public Dropdown genderDropdown;
    public Dropdown raceDropdown;
    ClothingItem[] clothingItems;
    string raceString;
    string genderString;
    Text pageText;
    
    //List<ClothingItemRenderer> clothingPanels = new List<ClothingItemRenderer>();
    int previewIndex = 0;
  
    // Use this for initialization
    void Start()
    {
        panelUpdater = this.GetComponent<WardrobePreviewPanelUpdater>();
        pageText = GameObject.Find("PageText").GetComponent<Text>();


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
            DOption option = new DOption(slot.ToString(), slot);
            option.userObject = slot;
            slotOptions.Add(option);
        }
        slotChangeDropdown.AddOptions(slotOptions.Cast<Dropdown.OptionData>().ToList());

        DBInst.progress += (m) => progress = m;
        DBInst.loadOrCallback((d) => {
            db = d;
            
            });
    }
    
    public void clickLeft()
    {
        previewIndex -= panelUpdater.getVisiblePanels();
        if (previewIndex < 0)
            previewIndex = 0;
        updatePreview();
    }
    public void clickRight()
    {
        previewIndex += panelUpdater.getVisiblePanels();
        if (previewIndex > clothingItems.Count()- panelUpdater.getVisiblePanels())
            previewIndex = clothingItems.Count() - panelUpdater.getVisiblePanels();
        updatePreview();
    }
    public void changeSlot()
    {
        DOption option = (DOption)slotChangeDropdown.options[slotChangeDropdown.value];
        GearSlot slot = (GearSlot)option.userObject;
        clothingItems = db.getClothing().Where(c => c.allowedSlots.Contains(slot)).ToArray();
        previewIndex = 0;
        updatePageText();
        updatePreview();
    }

    void updatePageText()
    {
        pageText.text = "Items " + previewIndex + "-" + (previewIndex + panelUpdater.getVisiblePanels()) + " of " + clothingItems.Length;
    }
    public void mainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("test-decomp");

    }
    void updatePreview()
    {
       // Debug.Log("update preview");
        updatePageText();
        ClothingItemRenderer[] renderers = panelUpdater.getPanelRenderers();
        //Debug.Log("update preview: renderers[" + renderers.Count() + "]");
        for (int i = 0; i < renderers.Length; i++)
        {
            ClothingItem item = clothingItems[previewIndex + i];
            //Debug.Log("set panel[" + i + "] to " + item);
            renderers[i].setItem(item);
        }
        lastVisible = panelUpdater.getVisiblePanels();
    }

    public void updateRaceGender()
    {
        raceString = raceDropdown.options[raceDropdown.value].text;
        genderString = genderDropdown.options[genderDropdown.value].text;

        paperDoll.setGender(genderString);
        paperDoll.setRace(raceString);
        //paperDoll.updateRaceGender();

        ClothingItemRenderer[] renderers = panelUpdater.getPanelRenderers();
        foreach (ClothingItemRenderer r in renderers)
        {
            if (r.previewPaperdoll != null)
            {
                r.previewPaperdoll.setGender(genderString);
                r.previewPaperdoll.setRace(raceString);
                r.refresh();
            }
        }
        // reapply the costume
        changeAppearance();
    }

    bool first = false;
    int lastVisible = 0;
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
                options.Add(new DOption("", null));

                foreach (entry e in db.getEntriesForID(7638))
                {
                    CObject _7637 = db.toObj(e.id, e.key);
                    string str = _7637.getMember(0).convert().ToString();
                    DOption option = new DOption(str, e);
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
        if (db != null && lastVisible != this.panelUpdater.getVisiblePanels() || this.panelUpdater.changed)
        {
            updatePreview();
            lastVisible = panelUpdater.getVisiblePanels();
        }



    }

    public void changeAppearance()
    {
        if (appearanceDropdown.options.Count == 0)
            return;
        int v = appearanceDropdown.value;
        DOption option = (DOption)appearanceDropdown.options[v];
        if (option.userObject == null)
        {
            paperDoll.clearAppearence();
        }
        else
        {
            entry entry = (entry)option.userObject;
            paperDoll.setAppearenceSet(entry.key);
        }
    }

  

    class DOption : Dropdown.OptionData
    {
        public DOption(string str, object usrObj)
        {
            base.text = str;
            this.userObject = usrObj;
        }
        public object userObject { get; set; }
    }
}

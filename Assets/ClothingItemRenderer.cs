using Assets.Database;
using Assets.RiftAssets;
using Assets.Wardrobe;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class ClothingItemRenderer : MonoBehaviour {
    DB db;
    
    public GameObject previewsRoot;
    GameObject ourPreview;
    Paperdoll mainPaperdoll;
    public Paperdoll previewPaperdoll;
    ClothingItem item;
    public int previewIndex;
    Text itemText;

    // Use this for initialization
    void Start() {
    }
    public void OnDestroy()
    {
        GameObject.Destroy(ourPreview);
    }
    public void init()
    { 
       
        itemText = GameObject.Find("ItemNameText").GetComponent<Text>();
        previewsRoot = GameObject.Find("PreviewsRoot");
        DBInst.loadOrCallback((d) => db = d);
        mainPaperdoll = GameObject.Find("MainPaperdoll").GetComponent<Paperdoll>();
        ourPreview = new GameObject();
        ourPreview.transform.parent = previewsRoot.transform;

        EventTrigger trigger = this.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => { itemClicked(); });
        trigger.triggers.Add(entry);

        EventTrigger.Entry eentry = new EventTrigger.Entry();
        eentry.eventID = EventTriggerType.PointerEnter;
        eentry.callback.AddListener((data) => { itemEntered(); });
        trigger.triggers.Add(eentry);

    }
    public void itemEntered()
    {
        if (item != null)
        {
            string str = "";
            if (DBInst.lang_inst != null)
                str = DBInst.lang_inst.get(item.langKey);
            itemText.text = str + "(" + item.name + ")";
        }
    }
    public void itemClicked()
    {
        if (item != null)
        {
            WardrobePreviewPanelUpdater wppu = GameObject.Find("Wardrobe").GetComponent<WardrobePreviewPanelUpdater>();
            Toggle toggle = GetComponentInChildren<Toggle>();

            GearSlot slot = item.allowedSlots.First();
            if (!mainPaperdoll.slotSet(slot))
            {
                mainPaperdoll.setGearSlotKey(slot, item.key);
                wppu.toggle(this, true);
            }
            else
            {
                long oldKey = mainPaperdoll.getGearSlot(slot);
                if (oldKey == item.key)
                {
                    mainPaperdoll.clearGearSlot(slot);
                    wppu.toggle(this, false);
                }
                else
                {
                    mainPaperdoll.setGearSlotKey(slot, item.key);
                    wppu.toggle(this, true);
                }
            }
        }
    }
    public void refresh()
    {
        ClothingItem t = this.item;
        this.item = null;
        setItem(t, mainPaperdoll);
    }
  
    public void setItem(ClothingItem item, Paperdoll mainDoll)
    {
        Debug.Log("set item [" + item + "] and copy others from mainDoll", this.gameObject);
        this.item = item;
        ourPreview.transform.Clear();
        if (previewPaperdoll == null)
            previewPaperdoll = ourPreview.AddComponent<Paperdoll>();
        ourPreview.name = "PaperDoll" + previewIndex;
        //GameObject.Destroy(previewPaperdoll);
        previewPaperdoll.setGender(mainPaperdoll.getGenderString());
        previewPaperdoll.setRace(mainPaperdoll.getRaceString());
        // start isn't called until the next "update" so we need to start it manually
        string nifstr = Path.GetFileName(item.nifRef.getNif(1, 0));
        ourPreview.name = item.name;
        Debug.Log("copy appearence from mainDoll", this.gameObject);
        previewPaperdoll.copy(mainDoll);
        previewPaperdoll.setGearSlotKey(item.allowedSlots.First(), item.key);


        SetLayerRecursively(ourPreview, LayerMask.NameToLayer("Preview" + previewIndex));
    }
    // Update is called once per frame
    bool first = false;
    void Update () {
		if (DBInst.loaded && !first)
        {
            first = true;
            db = DBInst.inst;
            if (item == null)
            {
                //IEnumerable<ClothingItem> clothing = db.getClothing();
                //item = clothing.First();
            }
            //setItem(item);
        }
        SetLayerRecursively(ourPreview, LayerMask.NameToLayer("Preview" + previewIndex));
    }

    public static void SetLayerRecursively(GameObject go, int layerNumber)
    {
        foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = layerNumber;
        }
    }

   
}

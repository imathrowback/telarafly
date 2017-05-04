using Assets.Database;
using Assets.RiftAssets;
using Assets.Wardrobe;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClothingItemRenderer : MonoBehaviour {
    DB db;
    AssetDatabase adb;
    NIFLoader loader;
    public GameObject previewsRoot;
    GameObject ourPreview;
    Paperdoll mainPaperdoll;
    public Paperdoll previewPaperdoll;
    ClothingItem item;
    public int previewIndex;

    // Use this for initialization
    void Start () {
        loader = new NIFLoader();
        loader.loadManifestAndDB();
        adb = loader.db;
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


    }

    public void itemClicked()
    {
        mainPaperdoll.setGear(item.allowedSlots.First(), item.key);
    }
    public void refresh()
    {
        setItem(item);
    }
    public void setItem(ClothingItem item)
    {
        this.item = item;
        ourPreview.transform.Clear();
        if (previewPaperdoll != null)
            GameObject.Destroy(previewPaperdoll);
        previewPaperdoll = ourPreview.AddComponent<Paperdoll>();
        previewPaperdoll.setGender(mainPaperdoll.getGenderString());
        previewPaperdoll.setRace(mainPaperdoll.getRaceString());
        // start isn't called until the next "update" so we need to start it manually
        previewPaperdoll.init();
        string nifstr = Path.GetFileName(item.nifRef.getNif(1, 0));
        ourPreview.name = item.name;

        previewPaperdoll.setGear(item.allowedSlots.First(), item.key);
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
	}

    public static void SetLayerRecursively(GameObject go, int layerNumber)
    {
        foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = layerNumber;
        }
    }
}

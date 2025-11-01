using Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Dropdown))]
public class FavDropDown : MonoBehaviour {
    public Sprite favSprite;
    public Sprite notFavSprite;
    Dropdown dropdown;
    bool itemsDirty = false;
    
    public string saveName = "globalFav";

    void Start() {
        DropDownItemIconScript itemIconScript = GetComponent<DropDownItemIconScript>();
        dropdown = GetComponent<Dropdown>();

        readFavs();
        setupArrowFavIcon();
        setupItemListFavIcon();

        dropdown.onValueChanged.AddListener((x) =>
        {
            if (itemsDirty)
            {
                DOption selected = (DOption)dropdown.options[x];
                doOptions();
                for (int i = 0; i < dropdown.options.Count; i++)
                {
                    DOption d = (DOption)dropdown.options[i];
                    if (d == selected)
                    {
                        dropdown.value = i;
                        dropdown.RefreshShownValue();
                        break;
                    }
                }
                storeFavs();
            }
            itemsDirty = false;

        });
    }
    public void readFavs()
    {
        try
        {
            byte[] saveData = Convert.FromBase64String(PlayerPrefs.GetString(saveName));
            BinaryFormatter ser = new BinaryFormatter();
            List<string> favs = (List<string>)ser.Deserialize(new MemoryStream(saveData));
                DOption[] options = dropdown.options.Cast<DOption>().ToArray();
                foreach (DOption i in options)
                    i.fav = favs.Contains(i.text);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Unable to process player preferences for favourites[" + saveName + "]:" + ex.Message);
        }
        doOptions();
    }

    void storeFavs()
    {
       
        List<string> favs = new List<string>();
        DOption[] options = dropdown.options.Cast<DOption>().ToArray();
        foreach (DOption i in options)
            if (i.fav)
                favs.Add(i.text);
        BinaryFormatter ser = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        ser.Serialize(ms, favs);
        ms.Seek(0, SeekOrigin.Begin);
        string str = Convert.ToBase64String(ms.ToArray());
        Debug.Log("writing player prefences to " + saveName);

        PlayerPrefs.SetString(saveName, str);
        PlayerPrefs.Save();
    }

    public void doOptions()
    {
        try
        {
            List<Dropdown.OptionData> opts = dropdown.options;
            List<DOption> options = opts.Cast<DOption>().ToList();
            foreach (DOption i in options)
            {
                if (i.fav)
                    i.image = favSprite;
                else
                    i.image = notFavSprite;
            }
            dropdown.ClearOptions();
            dropdown.AddOptions(options.OrderBy(x => !x.fav).ThenBy(x => x.text).Cast<Dropdown.OptionData>().ToList());
            dropdown.RefreshShownValue();
        }catch (Exception ex)
        {
            Debug.LogWarning("Unable to properly process items, are you sure they are DOption types in the drop down?" + ex);
        }
    }

    void FavButtonClicked(Dictionary<string, object> dict)
    {
        int index = (int)dict["index"];
        FavButton button = (FavButton)dict["source"];
        DOption option = (DOption)dropdown.options[index];
        Debug.Log("fav button clicked index:" + index + " source:" + button + " option:" + option.text);
        toggleFav(option);
        button.gameObject.GetComponent<Image>().sprite = option.image;
        dropdown.RefreshShownValue();
        itemsDirty = true;
        storeFavs();
    }

    void toggleFav(DOption option)
    {
        option.fav = !option.fav;
        if (option.fav)
            option.image = favSprite;
        else
            option.image = notFavSprite;
    }

    void setupItemListFavIcon()
    {
      
        GameObject label = this.transform.FindDeepChild("Item Label").gameObject;
        GameObject currentFaveImageObject = new GameObject("FaveItemListIcon");
        currentFaveImageObject.transform.parent = label.transform;
        Image currentFaveImageComponent = currentFaveImageObject.AddComponent<Image>();
        currentFaveImageComponent.sprite = favSprite;
        RectTransform rt = currentFaveImageObject.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(16, 16);
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        dropdown.itemImage = currentFaveImageComponent;


        FavButton butt = currentFaveImageObject.AddComponent<FavButton>();
        butt.clickReciever = this.gameObject;
        butt.clickMethodReciever = "FavButtonClicked";

        /*
        butt.onClick.AddListener(() =>
        {
            Debug.Log("but click");
            GameObject ButtonClicked = EventSystem.current.currentSelectedGameObject;
            Debug.Log(ButtonClicked);
            // fav icon clicked
            // which item are we hovering over?
           
        });
        
        // hook user clicking on the fav icon
        EventTrigger trigger = butt.gameObject.AddComponent<EventTrigger>();
        foreach (EventTriggerType t in Enum.GetValues(typeof(EventTriggerType)))
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = t;
                //EventTriggerType.PointerDown;
            entry.callback.AddListener((data) => {
                Debug.Log("[" + t + "]: tada!");
                Debug.Log(data);
                data.Use();
            });

            trigger.triggers.Add(entry);
        }
        */
        


    }




    void setupArrowFavIcon()
    {
       
        GameObject arrow = this.transform.Find("Arrow").gameObject;
        GameObject currentFaveImageObject = new GameObject("currentFaveImageIcon");
        currentFaveImageObject.transform.parent = arrow.transform;
        Image currentFaveImageComponent = currentFaveImageObject.AddComponent<Image>();
        currentFaveImageComponent.sprite = favSprite;
        RectTransform rt = currentFaveImageObject.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(16, 16);
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        dropdown.captionImage = currentFaveImageComponent;



    }

    // Update is called once per frame
    void Update () {
		
	}
}

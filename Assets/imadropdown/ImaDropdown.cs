using Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Linq;

public class ImaDropdown : MonoBehaviour
{
    public List<DOption> options { get;  }
    DOption selectedOption;
    Color normalColor = Color.white;
    Color overColor = new Color(0.7f, 0.7f, 0.7f);

    [SerializeField]
    GameObject selectedItemObject;

    private GameObject lastItemOver = null;
    GameObject dropList;

    [SerializeField]
    public Image captionImage { get; set; }
    public Image itemImage { get; internal set; }

    public Dropdown.DropdownEvent onValueChanged = new Dropdown.DropdownEvent();

    public ImaDropdown() : base()
    {
        options = new List<DOption>();
    }
	// Use this for initialization
	void Start () {
        
        dropList = transform.Find("DropList").gameObject;

        List<DOption> options = new List<DOption>();
        for (int i = 0; i < 6; i++)
        {
            options.Add(new DOption(i + "123", null, true));
            options.Add(new DOption(i + "1234", null));
            options.Add(new DOption(i + "1235", null));
            options.Add(new DOption(i + "1236", null, true));
        }
        SetOptions(options);

        makeTrigger(EventTriggerType.PointerEnter, (x) => OnPointerEnter((PointerEventData)x));
        makeTrigger(EventTriggerType.PointerClick, (x) => OnPointerClick((PointerEventData)x));
        makeTrigger(EventTriggerType.PointerExit, (x) => OnPointerExit((PointerEventData)x));

    }

    public void RefreshShownValue()
    {
        //Debug.Log("refresh shown value");
        selectedItemObject.GetComponent<Text>().text = selectedOption.text;
        if (captionImage != null)
        {
            Debug.Log("setting caption image");
            captionImage.sprite = null;
            if (selectedOption != null)
            {
                Debug.Log("set caption image");
                captionImage.sprite = selectedOption.image;
            }
        }
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void hide()
    {
        dropList.SetActive(false);
    }

    private EventTrigger.Entry makeTrigger(EventTriggerType type, UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry e = new EventTrigger.Entry();
        e.eventID = type;
        e.callback = new EventTrigger.TriggerEvent();
        e.callback.AddListener(action);
        trigger.triggers.Add(e);
        return e;
    }

    private Image getItemBackground(GameObject go)
    {
        Transform back = go.transform.parent.Find("Item Background");
        if (back != null)
            return back.gameObject.GetComponent<Image>();
        return null;
    }
    public void setSelected(DOption option)
    {
        this.selectedOption = option;
        RefreshShownValue();
        onValueChanged.Invoke(options.IndexOf(option));
    }

    public void OnPointerScroll(BaseEventData data)
    {
        Debug.Log(data.GetType());
        Debug.Log(data);
    }

    public void onItemEnter(BaseEventData data)
    {
        //if (lastItemOver != null)
        //    lastItemOver.GetComponent<Text>().color = normalColor;
        PointerEventData pdata = (PointerEventData)data;
        //Debug.Log("enter:" + data);
        GameObject itemLabel = pdata.pointerEnter;
        Image background = getItemBackground(itemLabel);
        if (background != null)
        {
            background.color = overColor;
        }
        lastItemOver = itemLabel;
    }

    public void onItemExit(BaseEventData data)
    {
        PointerEventData pdata = (PointerEventData)data;
        if (lastItemOver != null)
        {
            Image background = getItemBackground(lastItemOver);
            if (background != null)
                background.color = normalColor;
        }
        lastItemOver = null;
    }

    internal void SetOptions(IEnumerable<DOption> newOptions)
    {
        options.Clear();
        options.AddRange(newOptions);
        if (newOptions.Count() > 0)
            setSelected(options[0]);
        dropList.GetComponent<ImaScrollViewport>().setItems(options);
        if (this.itemImage != null)
            dropList.GetComponent<ImaScrollViewport>().itemImageName = this.itemImage.name;
    }

    public void onItemClick(BaseEventData data)
    {
        PointerEventData pdata = (PointerEventData)data;
        //Debug.Log("click:" + data);
        GameObject itemTemplate = pdata.pointerPress;
        ImaListItem listItem = itemTemplate.GetComponent<ImaListItem>();
        DOption option = (DOption)listItem.userObject;
        itemTemplate.transform.Find("Item Background").gameObject.GetComponent<Image>().color = normalColor;
        setSelected(option);
        hide();
    }

    public void OnPointerEnter(PointerEventData data)
    {
        GetComponent<Image>().color = overColor;
    }
    public void OnPointerClick(PointerEventData data)
    {
        if (dropList.activeInHierarchy)
            dropList.SetActive(false);
        else
        {

            dropList.SetActive(true);
            dropList.GetComponent<ImaScrollViewport>().setItems(options);
            // create blocker to detect mouse OFF clicks
            //GameObject blocker = new GameObject();
            //blocker.transform.parent = 
        }
    }
    public void OnPointerExit(PointerEventData data)
    {
        GetComponent<Image>().color = normalColor;
    }
}

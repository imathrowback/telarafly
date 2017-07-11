using System;

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Assets;
using UnityEngine;
using UnityEngine.UI;

public class ImaScrollViewport : MonoBehaviour {

    [SerializeField]
    List<DOption> options = new List<DOption>();

    Dictionary<int, GameObject> visibleItems = new Dictionary<int, GameObject>();

    public string itemImageName;

    [SerializeField]
    GameObject templateItem;

    int startVisibleIndex = 0;

    private float itemHeight()
    {
        return templateItem.GetComponent<RectTransform>().sizeDelta.y;
    }
    private int visibleItemCount()
    {
        return Mathf.FloorToInt(GetComponent<RectTransform>().sizeDelta.y / itemHeight());
    }
    void destroyItems()
    {
        foreach (GameObject go in visibleItems.Values)
            GameObject.Destroy(go);
        visibleItems.Clear();
    }
    void updateItems()
    {
        int y = 2;

        trimVisibleItems();
        for (int i = 0; i < visibleItemCount(); i++)
        {
            GameObject item;
            if (!visibleItems.TryGetValue(i, out item))
            {
                item = GameObject.Instantiate(templateItem, gameObject.transform);
                visibleItems[i] = item;
            }

            int optionIndex = startVisibleIndex + i;
            if (optionIndex < options.Count)
            {
                DOption option = options[optionIndex];
                Text text = item.GetComponentInChildren<Text>();
                text.text = option.text;
                item.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, (-i * itemHeight())-2);
                if (itemImageName != null)
                {
                    Transform itemImageObj = item.transform.FindDeepChild(itemImageName);
                    if (itemImageObj != null)
                    {
                        Image itemImage = itemImageObj.GetComponent<Image>();
                        if (itemImage != null)
                            itemImage.GetComponent<Image>().sprite = option.image;
                    }
                }
                item.SetActive(true);
                item.GetComponent<ImaListItem>().userObject = option;
                item.name = i + ":" +  text.text;
            }
            else
            {
                item.SetActive(false);
            }

        }

        // ensure the scrollbox is always the last sibling
        GetComponentInChildren<Scrollbar>().transform.SetAsLastSibling();

    }

    private void scrollbarChanged(float newValue)
    {

        startVisibleIndex = Mathf.FloorToInt((float)options.Count * newValue);
        updateItems();
    }

    private void trimVisibleItems()
    {
        foreach (int v in visibleItems.Keys.ToList())
        {
            if (v > visibleItemCount())
                visibleItems.Remove(v);
        }
    }

    // Use this for initialization
    void Start () {
        Scrollbar sb = GetComponentInChildren<Scrollbar>();
        sb.onValueChanged.AddListener(scrollbarChanged);

    }

    // Update is called once per frame
    void Update () {
		
	}

    internal void setItems(List<DOption> options)
    {
        this.options.Clear();
        this.options.AddRange(options);
        destroyItems();
        updateItems();

        // update the scrollbar size
        Scrollbar sb = GetComponentInChildren<Scrollbar>();
        sb.size = (float)visibleItems.Count / ((float)options.Count - visibleItems.Count);
       // sb.numberOfSteps = 1;// options.Count;

    }
}

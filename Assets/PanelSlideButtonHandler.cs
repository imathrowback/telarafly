using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelSlideButtonHandler : MonoBehaviour {
    public GameObject panel;
    enum PanelState {
        CLOSED, OPEN
    }

    public int minPanelSize = 20;
    public int maxPanelSize = 500;
    public int animSpeed = 50;

    PanelState state = PanelState.OPEN;

    public void buttonPressed()
    {
        if (state == PanelState.OPEN)
        {
            state = PanelState.CLOSED;
            setChildren(panel.transform, false);
        }
        else
        {
            state = PanelState.OPEN;
            setChildren(panel.transform, true);
        }
    }

    void setChildren(Transform t, bool state)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            if (t.GetChild(i).gameObject != this.gameObject)
                t.GetChild(i).gameObject.SetActive(state);
        }
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        RectTransform rect = panel.GetComponent<RectTransform>();
        if (state == PanelState.CLOSED)
        {
            //if (rect.rect.width > minPanelSize)
            {
                float newX = Mathf.Max(rect.sizeDelta.x - animSpeed, minPanelSize);
                rect.sizeDelta = new Vector2(newX, rect.sizeDelta.y);
            }
        }
        if (state == PanelState.OPEN)
        {
            //if (rect.rect.width < maxPanelSize)
            {
                float newX = Mathf.Min(rect.sizeDelta.x + animSpeed, maxPanelSize);
                rect.sizeDelta = new Vector2(newX, rect.sizeDelta.y);
            }
        }
    }
}

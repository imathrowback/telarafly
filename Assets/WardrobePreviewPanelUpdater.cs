using Assets.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WardrobePreviewPanelUpdater : MonoBehaviour {

    int panelCount = 9;
    Text loadingText;
    public GameObject previewPanel;
    public GameObject previewsRoot;
    public GameObject previewTemplatePrefab;

    GameObject[] panels = new GameObject[9];
    public bool changed = false;
    public int getVisiblePanels()
    {
        RectTransform rtPanel = this.GetComponent<RectTransform>();
        Canvas canvas = this.GetComponentInParent<Canvas>();
        float canvaswidth = ((rtPanel.rect.width * canvas.scaleFactor));
        return Mathf.FloorToInt(canvaswidth / 128);
    }
    // Use this for initialization
    void Start () {
        loadingText = GameObject.Find("LoadingText").GetComponent<Text>();
	}
    public ClothingItemRenderer[] getPanelRenderers()
    {
        return previewPanel.GetComponentsInChildren<ClothingItemRenderer>();
    }
    void buildPanels()
    {

        for (int i = 0; i < 9; i++)
        {
            int index = i + 1;
            GameObject go = GameObject.Instantiate(previewTemplatePrefab);
            panels[i] = go;
            go.tag = "ItemPreviewPanel";
            RectTransform rt = go.GetComponent<RectTransform>();
            float width = rt.sizeDelta.x;
            go.transform.SetParent(previewPanel.transform, false);
            //go.transform.localPosition = new Vector3(width * i, 0, 0);
            rt.anchoredPosition = new Vector2(width * i, 0);
            go.name = "ItemPreviewPanel" + index;

            RawImage img = go.GetComponent<RawImage>();
            GameObject prevCameraObj = GameObject.Find("PreviewCamera" + index);
            Camera camObj = prevCameraObj.GetComponent<Camera>();
            img.texture = camObj.targetTexture;

            ClothingItemRenderer renderer = go.GetComponent<ClothingItemRenderer>();
            renderer.previewIndex = index;
            renderer.previewsRoot = previewsRoot;
            renderer.init();
        }
    }
    int lastVisible = 0;
	// Update is called once per frame
	void Update () {
        changed = false;
        if (DBInst.loaded && loadingText.enabled)
        {
            loadingText.enabled = false;
            buildPanels();
            changed = true;
        }

        if (DBInst.loaded && lastVisible != getVisiblePanels())
        {
            //Debug.Log("lastVisible[" + lastVisible + "], vis[" + getVisiblePanels() + "]");
            for (int i = 0; i < 9; i++)
            {
                GameObject go = this.panels[i];
                ClothingItemRenderer renderer = go.GetComponent<ClothingItemRenderer>();
                if (i < getVisiblePanels())
                {
                    //Debug.Log("active[" + i + "]:" + go.tag);
                    go.SetActive(true);
                    if (renderer.previewPaperdoll != null)
                        renderer.previewPaperdoll.gameObject.SetActive(true);
                }
                else
                {
                    //Debug.Log("inactive[" + i + "]:" + go.tag);
                    go.SetActive(false);
                    if (renderer.previewPaperdoll != null)
                        renderer.previewPaperdoll.gameObject.SetActive(false);
                }
            }
            lastVisible = getVisiblePanels();
            changed = true;
        }
    }
}

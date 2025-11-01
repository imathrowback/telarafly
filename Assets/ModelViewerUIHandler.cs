using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModelViewerUIHandler : MonoBehaviour
{
    public Toggle ShowMountsOnly;
    public ImaDropdown modelChoiceDropdown;
    public Dropdown animDropdown;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void onClickModelChoiceDropdown()
    {
        modelChoiceDropdown.OnPointerClick(null);
    }
    public void onClickShowMounts()
    {
        ShowMountsOnly.OnPointerClick(null);
    }
}

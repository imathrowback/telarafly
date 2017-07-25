using Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class dropdowntest : MonoBehaviour {
    public ImaDropdown d;
	// Use this for initialization
	void Start () {

       List<DOption> options = new List<DOption>();
       for (int i = 0; i < 1116; i++)
       {
           options.Add(new DOption(i + "123", null, true));
           options.Add(new DOption(i + "1234", null));
           options.Add(new DOption(i + "1235", null));
           options.Add(new DOption(i + "1236", null, true));
       }
        d.GetComponent<FavDropDown2>().setSortedOptions(options);
	}
	
	// Update is called once per frame
	void Update () {
        //GetComponent<Text>().text = "" + d.value + "-" + d.options[d.value].text;
	}
}

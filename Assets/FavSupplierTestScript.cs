using Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class FavSupplierTestScript : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        Dropdown d = GetComponent<Dropdown>();
        d.ClearOptions();
        var options = d.options;

        for (int i = 0; i < 6; i++)
        {
            options.Add(new DOption(i + "123", null, true));
            options.Add(new DOption(i  + "1234", null));
            options.Add(new DOption(i + "1235", null));
            options.Add(new DOption(i + "1236", null, true));
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
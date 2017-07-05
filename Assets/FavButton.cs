using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets
{
    public class FavButton : Button
    {
        public GameObject clickReciever;
        public string clickMethodReciever;

        override public void OnPointerClick(PointerEventData eventData)
        {
            /** so terribad */
            Toggle toggle = this.transform.GetComponentInParent<Toggle>();
            string value = toggle.name;
            int id = int.Parse(value.Split(':')[0].Split(' ')[1]);
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["index"] = id;
            dict["source"] = this;

            clickReciever.SendMessage(clickMethodReciever, dict);
        }
    }
}

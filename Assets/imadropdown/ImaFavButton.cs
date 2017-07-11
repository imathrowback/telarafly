using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets
{
    public class ImaFavButton : Button
    {
        public GameObject clickReciever;
        public string clickMethodReciever;

        override public void OnPointerClick(PointerEventData eventData)
        {
            /** so terribad */
            Debug.Log(eventData);
            ImaListItem toggle = this.transform.GetComponentInParent<ImaListItem>();
            string value = toggle.name;
            int id = int.Parse(value.Split(':')[0]);
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["index"] = id;
            dict["source"] = this;

            clickReciever.SendMessage(clickMethodReciever, dict);
        }
    }
}

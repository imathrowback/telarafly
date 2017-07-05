using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace Assets
{
    public class DOption : Dropdown.OptionData
    {
        public DOption(string str, object usrObj, bool fav = false)
        {
            this.fav = fav;
            base.text = str;
            this.userObject = usrObj;
        }
        public bool fav { get; set; }
        public object userObject { get; set; }
    }

    public interface FavSupplierInterface
    {
        DOption[] getOptions();
    }
}

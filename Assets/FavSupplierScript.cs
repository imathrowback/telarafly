using System;
using System.Collections;
using System.Collections.Generic;
using Assets;
using UnityEngine;

public abstract class FavSupplierScript : MonoBehaviour, Assets.FavSupplierInterface {

    public abstract DOption[] getOptions();


}



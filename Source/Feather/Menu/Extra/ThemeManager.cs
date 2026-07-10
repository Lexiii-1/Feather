using Feather.Menu.Backend;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UnityEngine;

namespace Feather.Menu.Extra
{
    public class ThemeManager
    {
        public static UnityEngine.Color GetTheme()
        {
            if (MenuBackend.Enabled("Menu Settings", "Match Asset Bundle Theme"))
            {
                return AssetBundleLoader.MenuInstance.transform.Find("Back").GetComponent<Renderer>().sharedMaterial.color;
            }
            else
            {
                return VRRig.LocalRig.playerColor;
            }
        }
    }
}

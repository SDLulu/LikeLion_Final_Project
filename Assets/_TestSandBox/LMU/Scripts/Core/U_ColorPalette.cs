using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore
{
    //
    //
    //        [GUIColor(U_ColorPalette.GreenHex)]
    //        [Button("이름", ButtonSizes.Large)]

    public static class U_ColorPalette 
    {
        /// <summary> 연한 초록색 </summary>
        public const string GreenHex = "#80ed99";

        /// <summary> 연한 빨간색 </summary>
        public const string RedHex = "#ffb3c6";

        /// <summary> 연한 파란색 </summary>
        public const string BlueHex = "#a2d2ff";

        /// <summary> 연한 보라색 </summary>
        public const string BoraHex = "#c8b6ff";

        public static Color GetHexColor(string html)
        {
            Color color;
            ColorUtility.TryParseHtmlString(html, out color);
            color.a = 1f;
            return color;
        }
    }
}
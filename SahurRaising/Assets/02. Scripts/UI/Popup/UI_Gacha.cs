using SahurRaising.UI;
using UnityEngine;

namespace SahurRaising
{
    public class UI_Gacha : UI_Popup
    {
        public void OnClickBack()
        {
            UIManager.Instance.CloseCurrentPopup();
        }
    }
}

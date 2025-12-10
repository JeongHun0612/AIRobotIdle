using SahurRaising.UI;
using UnityEngine;

namespace SahurRaising
{
    public class UI_Equipment : UI_Popup
    {
        public void OnClickGacha()
        {
            UIManager.Instance.ShowPopup(EPopupUIType.Gacha);
        }

        public void OnClickUpgrade()
        {
            Debug.Log("[UI_Equipment] OnClickUpgrade");
        }
    }
}

using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    public class EquipmentInfo : MonoBehaviour
    {
        [SerializeField] private EquipmentInfoItemSlot _itemSlot;

        public void Show()
        {
            gameObject.SetActive(true);

        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void RefreshEquipmentInfo(EquipmentRow data)
        {
            _itemSlot.SetData(data);
        }
    }
}

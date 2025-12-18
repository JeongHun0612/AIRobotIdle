using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    public class EquipmentInfoItemSlot : ItemSlot
    {
        public override void SetData(EquipmentRow data)
        {
            base.SetData(data);

            _slotButton.interactable = false;
            _equipToggleButton.interactable = false;
        }
    }
}
